
using DriveDownloader.Worker.Models;
using Microsoft.EntityFrameworkCore;
using SimpleGoogleDrive;
using SimpleGoogleDrive.Models;

namespace DriveDownloader.Worker;

internal static class DbHelpers
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
    public static async Task Load(GoogleDriveService drive, string googleDriveBaseFolder, CancellationToken token)
    {
        Console.WriteLine($"Parsing files in {googleDriveBaseFolder}...");
        var folder = await drive.FindFolder(googleDriveBaseFolder,token: token);

        if (folder is null)
        {
            Console.WriteLine("The folder does not exists!");
            ArgumentNullException.ThrowIfNull(folder);
        }
        
        
        var query = new QueryBuilder().IsNotType(DriveResource.MimeType.Folder).And().IsNotType(DriveResource.MimeType.GoogleDriveShortcut); //.And().HasNotPropertyValue("is downloaded","true");
        var resources = folder.GetInnerResources(query, deepSearch: true, token);

        await foreach (var resource in resources.WithCancellation(token))
        {
            await using var db = new DriveDBContext();
            try
            {
                if (resource.Size is null && resource.Type.GetDefaultExportType() is default(DriveResource.MimeType))
                    continue;
                
                if (await db.Files.AnyAsync(t => t.Id == resource.Id, token))
                    continue;

                var a = await resource.GetFullName(token);

                // Console.WriteLine(a);

                await db.Files.AddAsync(new GoogleDriveFile
                    {
                        Id = resource.Id,
                        State = FileState.NotDownloaded,
                        FullPath = a,
                        TotalBytes = resource.Size ?? 0
                    },
                    token
                );
                await db.SaveChangesAsync(token);
            }
            catch (Exception e)
            {
                Errors error = new Errors
                {
                    DateTime = DateTime.Now,
                    Details = $"Filename: {resource.Name} \nID: {resource.Id}",
                    Stacktrace = e.StackTrace ?? "",
                    Function = "Fetch",
                    Message = e.Message,
                };

                await db.Errors.AddAsync(error, token);
                await db.SaveChangesAsync(token);
            }

        }
    }
}