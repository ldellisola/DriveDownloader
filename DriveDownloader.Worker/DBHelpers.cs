
using DriveDownloader.Worker.Models;
using Google.Apis.Drive.v3;
using Microsoft.EntityFrameworkCore;
using SimpleGoogleDrive;
using SimpleGoogleDrive.Models;

namespace DriveDownloader.Worker;

internal class DBHelpers
{
    public static async Task<Status> Init()
    {
        await using var db = new DriveDBContext();
        await db.Database.EnsureCreatedAsync();

        var status = db.Status.FirstOrDefault() ?? new Status();

        if (status.AppName is null)
        {
            Console.WriteLine("Application Name:");
            status.AppName = Console.ReadLine();
        }

        if (status.ClientId is null)
        {
            Console.WriteLine("Client Id:");
            status.ClientId = Console.ReadLine();
        }

        if (status.ClientSecret is null)
        {
            Console.WriteLine("Client Secret:");
            status.ClientSecret = Console.ReadLine();
        }

        if (status.LocalBaseFolder is null)
        {
            Console.WriteLine("Local folder to download all files");
            status.LocalBaseFolder = Console.ReadLine();
        }


        if (status.RemoteBaseFolder is null)
        {
            Console.WriteLine("Google Drive folder where the files are");
            status.RemoteBaseFolder = Console.ReadLine();
        }

        if (!db.Status.Any())
            db.Status.Add(status);
        
        await db.SaveChangesAsync();
        return status;
    }
    public static async Task RestoreDownloadingFiles()
    {
        // restauro el estado para las descargas que no se completaron
        
        Console.WriteLine("Fixing files that did not finished downloading...");
        
        await using var db = new DriveDBContext();
        var files = db.Files.Where(t => t.State == FileState.Downloading);
        foreach (var googleDriveFile in files)
        {
            googleDriveFile.State = FileState.NotDownloaded;
            googleDriveFile.BytesDownloaded = 0;
        }

        await db.SaveChangesAsync();
    }
    public static async Task Load(GoogleDriveService drive, string googleDriveBaseFolder)
    {
        Console.WriteLine($"Parsing files in {googleDriveBaseFolder}. This may take a while...");
        var folder = await drive.FindFolder(googleDriveBaseFolder);
        ArgumentNullException.ThrowIfNull(folder);
        var query = new QueryBuilder().IsNotType(DriveResource.MimeType.Folder); //.And().HasNotPropertyValue("is downloaded","true");
        var resources = (await folder.GetInnerResources(query, deepSearch: true)).ToArray();
        var ids = resources.Select(t => t.Id);

// Cargo la base de datos
        await using (var db = new DriveDBContext())
        {
            var usedIds = await db.Files.Where(t => ids.Contains(t.Id)).Select(t=> t.Id).ToArrayAsync();
            var unusedIds = ids.Where(t => !usedIds.Contains(t)).ToArray();

            await db.Files.AddRangeAsync(
                resources.Where(t => unusedIds.Contains(t.Id))
                    .Where(t=> t.Size is not null || t.Type.GetDefaultExportType() is not default(DriveResource.MimeType))
                    .Select(t=> new GoogleDriveFile
                        {
                            Id = t.Id,
                            State = FileState.NotDownloaded,
                            FullPath = t.GetFullName().Result,
                            TotalBytes = t.Size ?? 0
                        }
                    )
            );

            await db.SaveChangesAsync();
        }
    }
}