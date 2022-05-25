namespace DriveDownloader.Worker.Models;

public class Status
{
    public int Id { get; set; } = 1;
    public bool IsRunning { get; set; } = true;
    public int DownloadThreads { get; set; } = 2;
    public string? LocalBaseFolder { get; set; }
    public string? RemoteBaseFolder { get; set; }
    public string? AppName { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}