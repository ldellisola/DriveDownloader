using System.Text;
using System.Text.RegularExpressions;
using DriveDownloader.Worker.Models;

namespace DriveDownloader.Worker.CLI.Commands;

internal class StatusCommand : ICommand
{
    private readonly Regex _pattern = new ("^status$", RegexOptions.Compiled);
    public string CommandText => "status";

    private string GetCurrentDownload(GoogleDriveFile file)
    {
        int progress = (int) (file.BytesDownloaded * 30 / file.TotalBytes);
        return new StringBuilder()
            .Append('[')
            .Append(Enumerable.Range(0, progress).Aggregate("", (a, _) => a + '='))
            .Append(Enumerable.Range(0, 30 - progress).Aggregate("", (a, _) => a + ' '))
            .Append(']')
            .Append($"\t{FormatStorage(file.BytesDownloaded)}/{FormatStorage(file.TotalBytes)}")
            .Append($"\t{file.FullPath}")
            .ToString();
    }

    private string FormatStorage(long sizeInBytes) =>
        sizeInBytes switch
        {
            > (long) 1e12 => $"{sizeInBytes / 1e12:F4} TB",
            > (long) 1e9 => $"{sizeInBytes / 1e9:F3} GB",
            > (long) 1e6 => $"{sizeInBytes / 1e6:F2} MB",
            > (long) 1e3 => $"{sizeInBytes / 1e3:F1} KB",
            _ => $"{sizeInBytes:F4} B"
        };
    
    public string Execute(string command)
    {
        using var db = new DriveDBContext();
        var status = db.Status.FirstOrDefault() ?? new Status();
        var builder = new StringBuilder();

        builder.AppendLine(status.IsRunning ? "The service is running" : "The service is not running");
        builder.AppendLine($"There are {status.DownloadThreads} download threads running");
        builder.AppendLine();
        var currentDownloads = db.Files.Where(t => t.State == FileState.Downloading).ToArray()
                .Select(t => GetCurrentDownload(t));
        builder.AppendJoin("\n", currentDownloads);
        builder.AppendLine();
        builder.AppendLine(
            $"Downloaded {db.Files.LongCount(t => t.State == FileState.Downloaded)} out of {db.Files.LongCount()} files");
        builder.AppendLine(
            $"Downloaded {FormatStorage(db.Files.Sum(t => t.BytesDownloaded))} out of {FormatStorage(db.Files.Sum(t => t.TotalBytes))}");

        return builder.ToString();
    }

    public bool CanExecute(string command)
        => _pattern.IsMatch(command);

    public string Explain()
        => new StringBuilder()
            .AppendLine("Usage => status")
            .AppendLine("It will print the current state of the downloader")
            .ToString();
}