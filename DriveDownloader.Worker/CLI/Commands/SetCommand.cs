using System.Text;
using System.Text.RegularExpressions;
using SimpleGoogleDrive;
using SQLitePCL;

namespace DriveDownloader.Worker.CLI.Commands;

internal class SetCommand : ICommand
{
    private GoogleDriveService? _drive = null;
    private Regex _pattern = new(@"^set\s+(?<parameter>DownloadThreads|LocalFolder|RemoteFolder)\s+(?<value>.+)$",RegexOptions.Compiled);
    public string CommandText => "set";

    public SetCommand(GoogleDriveService? drive = null)
    {
        _drive = drive;
    }

    public string Execute(string command)
    {
        var match = _pattern.Match(command);
        using var db = new DriveDBContext();
        var status = db.Status.First();

        switch (match.Groups["parameter"])
        {
            case {Value: "DownloadThreads"}:
                if (!int.TryParse(match.Groups["value"].Value, out var value) || value < 0)
                    return $"Invalid parameter: {match.Groups["value"].Value}";

                status.DownloadThreads = value;
                db.SaveChanges();
                return string.Empty;
            case {Value: "LocalFolder"}:
                if (!Directory.Exists(match.Groups["value"].Value))
                    return $"The folder {match.Groups["value"].Value} does not exist";

                status.LocalBaseFolder = match.Groups["value"].Value;
                db.SaveChanges();
                return string.Empty;
            case {Value: "RemoteFolder"}:
                if (_drive?.FindFolder(match.Groups["value"].Value) is null)
                    return $"The folder {match.Groups["value"].Value} does not exists";
                TaskStorage.RunAndStore(c=>DbHelpers.Load(_drive, match.Groups["value"].Value,c));
                return string.Empty;
            default:
                return $"Invalid Parameter: {match.Groups["parameter"].Value}";
        }
        
    }

    public bool CanExecute(string command)
    {
        return _pattern.IsMatch(command);
    }

    public string Explain()
    {
        return new StringBuilder()
            .AppendLine("Usage => set PARAM VALUE")
            .AppendLine("It will set the values for the following parameters:")
            .AppendLine(
                "\t- DownloadThreads: It will set the number of threads downloading files. It must be an integer greater or equal than 0")
            .AppendLine("\t- LocalFolder: It will set the folder on your computer where to download the files")
            .AppendLine("\t- RemoteFolder: It will set the folder on Google Drive to scan for files")
            .ToString();
    }
}