using System.Text;
using System.Text.RegularExpressions;
using DriveDownloader.Worker.Models;

namespace DriveDownloader.Worker.CLI.Commands;

internal class RetryCommand : ICommand
{
    public string CommandText => "retry";
    private readonly Regex _pattern = new(@"^retry\s+(?<action>failed|all|\S.+)$", RegexOptions.Compiled);
    public string Execute(string command)
    {
        using var db = new DriveDBContext();
        var match = _pattern.Match(command);

        switch (match.Groups["action"])
        {
            case {Value: "failed", Success: true}:
            {
                var failedDownloads = db.Files.Where(t => t.State == FileState.Error).ToArray();
                foreach (var file in failedDownloads)
                {
                    file.State = FileState.NotDownloaded;
                }
                db.Files.UpdateRange(failedDownloads);
                db.SaveChanges();
                break;
            }
            case {Value: "all", Success: true}:
                var all = db.Files.Where(t => t.State != FileState.NotDownloaded).ToArray();
                foreach (var file in all)
                {
                    file.State = FileState.NotDownloaded;
                }
                db.Files.UpdateRange(all);
                db.SaveChanges();
                break;
            case {Success: true}:
                var one = db.Files.FirstOrDefault(t => t.Id == match.Groups["action"].Value);
                if (one is not null) 
                    one.State = FileState.NotDownloaded;
                db.SaveChanges();
                break;
            default:
                return "Invalid action";
        }

        return "Ok";
    }

    public bool CanExecute(string command)
        => _pattern.IsMatch(command);

    public string Explain()
        => new StringBuilder()
            .AppendLine("Usage => rety ID|failed|all")
            .AppendLine("\tIt retries a specific download, only failed downloads or all the files")
            .ToString();
}