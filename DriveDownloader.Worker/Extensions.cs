using SimpleGoogleDrive.Models;

namespace DriveDownloader.Worker;

public static class Extensions
{
    public static string GetExtension(this DriveResource.MimeType type)
        => type switch
        {
            DriveResource.MimeType.MsPowerPoint => ".pptx",
            DriveResource.MimeType.MsExcel => ".xlsx",
            DriveResource.MimeType.MsWord => ".docx",
            DriveResource.MimeType.GoogleAppScript => ".json",
            DriveResource.MimeType.GoogleDrawing => ".png",
            _ => ".unknown"
        };

    public static string SanitizeFileName(this string str)
    {
        var invalids = Path.GetInvalidFileNameChars();
        return string.Join("_", str.Split(invalids, StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');
    }
}