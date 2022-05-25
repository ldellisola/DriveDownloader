using System.Text;
using System.Text.RegularExpressions;
using DriveDownloader.Worker.Models;
using SimpleGoogleDrive;
using SQLitePCL;

namespace DriveDownloader.Worker.CLI.Commands;

internal class GetCommand : ICommand
{

    private readonly Regex _pattern = new(@"^get\s+(?<parameter>DownloadThreads|LocalFolder|RemoteFolder|AppName|ClientId|ClientSecret|Errors)$",
        RegexOptions.Compiled);

    public string CommandText => "get";
    

    public string Execute(string command)
    {
        var match = _pattern.Match(command);
        using var db = new DriveDBContext();
        var status = db.Status.First();

        return match.Groups["parameter"].Value switch
        {
            "DownloadThreads" => status.DownloadThreads.ToString(),
            "LocalFolder" => status.LocalBaseFolder ?? "Empty",
            "RemoteFolder" => status.RemoteBaseFolder ?? "Empty",
            "AppName" => status.AppName ?? "Empty",
            "ClientId" => status.ClientId ?? "Empty",
            "ClientSecret" => status.ClientSecret ?? "Empty",
            "Errors" => string.Join("\n", db.Files.Where(t=> t.State == FileState.Error).Select(t=> $"ID: {t.Id}, Name: {t.FullPath}")),
            _ => $"Invalid parameter: {match.Groups["parameter"].Value}"
        };
    }

    public bool CanExecute(string command) => _pattern.IsMatch(command);

    public string Explain()
    {
        return new StringBuilder()
            .AppendLine("Usage => get PARAM VALUE")
            .AppendLine("It will retrieve the values for the following parameters:")
            .AppendLine(
                "\t- DownloadThreads: It will get the number of threads downloading files")
            .AppendLine("\t- LocalFolder: It get set the folder on your computer where to download the files")
            .AppendLine("\t- RemoteFolder: It will get the folder on Google Drive to scan for files")
            .AppendLine("\t- AppName: It will get the Application Name registered in the Google Could Console")
            .AppendLine("\t- ClientId: It will get the ClientID registered in the Google Could Console")
            .AppendLine("\t- ClientSecret: It will get the ClientSecret registered in the Google Could Console")
            .AppendLine("\t- Errors: It prints all the files with errors")
            .ToString();
    }
}