using SimpleGoogleDrive.Models;

namespace DriveDownloader.Worker;

public static class Extensions
{
    public static string GetExtension(this DriveResource.MimeType type)
        => type switch
        {
            DriveResource.MimeType.MsPowerPoint => ".pptx",
            DriveResource.MimeType.MSExcel => ".xlsx",
            DriveResource.MimeType.MSWord => ".docx",
            DriveResource.MimeType.GoogleAppScript => ".json",
            DriveResource.MimeType.GoogleDrawing => ".png",
            _ => ".unknown"
        };
}