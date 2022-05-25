namespace DriveDownloader.Worker.Models;


public enum FileState
{
    NotDownloaded,
    Downloading,
    Downloaded,
    Deleting,
    Deleted,
    Error
}


public class GoogleDriveFile
{
    public string Id { get; set; }
    public FileState State { get; set; }
    public string FullPath { get; set; }
    public long TotalBytes { get; set; }
    public long BytesDownloaded { get; set; }

}