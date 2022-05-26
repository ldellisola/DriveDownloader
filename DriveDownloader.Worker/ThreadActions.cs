using DriveDownloader.Worker.Models;
using Microsoft.EntityFrameworkCore;
using SimpleGoogleDrive;
using SimpleGoogleDrive.Models;

namespace DriveDownloader.Worker;

internal static class ThreadActions
{
    public static async Task ManageDownloadThreads(GoogleDriveService drive, CancellationToken token)
    {
        var downloadTasks = new List<Task>();
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            await using (var db = new DriveDBContext())
            {
                var status = await db.Status.FirstOrDefaultAsync(token) ?? new Status();

                if (!status.IsRunning || status.DownloadThreads == 0)
                    continue;

                if (downloadTasks.Count >= status.DownloadThreads)
                {
                    var finishedTask = downloadTasks.FirstOrDefault(t => t.IsCompleted);
                    if (finishedTask is not null)
                        downloadTasks.Remove(finishedTask);
                    continue;
                }

                var newTask = Download(drive, status.LocalBaseFolder!, token);
                downloadTasks.Add(newTask);
            }
        }

        Task.WaitAll(downloadTasks.ToArray(), token);
    }

    private static async Task Download(GoogleDriveService drive, string localBasePath,
        CancellationToken token = default)
    {
        await using var db = new DriveDBContext();
        var file = await db.Files.Where(t => t.State == FileState.NotDownloaded)
            .FirstOrDefaultAsync(token);
        if (file is null)
            return;
        
        try
        {
            file.State = FileState.Downloading;
            await db.SaveChangesAsync(token);

            var resource = await drive.GetResource(file.Id, token);

            var newName = $"{localBasePath}/{file.FullPath}"
                    .FormatPath()
                    .Split("/", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.SanitizeFileName())
                    .Aggregate("", (a, b) => $"{a}/{b}")
                ;

            void OnProgress(long uploaded, long? total)
            {
                file.BytesDownloaded = uploaded;
                db.SaveChanges();
            }

            void OnFailure(Exception exception)
            {
                file.State = FileState.Error;
            }

            if (resource.Size is null && resource.Type.GetDefaultExportType() is not default(DriveResource.MimeType))
            {
                var type = (DriveResource.MimeType) resource.Type.GetDefaultExportType()!;

                await resource.Export(
                    $"{newName}{type.GetExtension()}",
                    type,
                    OnProgress,
                    OnFailure,
                    token
                );
            }
            else if (resource.Size is null && resource.Type.GetDefaultExportType() is default(DriveResource.MimeType))
            {
                file.State = FileState.Error;
            }
            else
            {
                await resource.Download(newName, OnProgress, OnFailure, token);
            }


            if (file.State is not FileState.Error)
            {
                file.BytesDownloaded = file.TotalBytes;
                file.State = FileState.Downloaded;
                // resource.Properties["is downloaded"] = "true";
                await resource.Update(token);
            }


            await db.SaveChangesAsync(token);
        }
        catch (Exception e)
        {
            Errors error = new Errors
            {
                DateTime = DateTime.Now,
                Details = $"Filename: {file.FullPath} \nID: {file.Id}",
                Stacktrace = e.StackTrace ?? "",
                Function = "Download",
                Message = e.Message,
            };

            await db.Errors.AddAsync(error, token);
            await db.SaveChangesAsync(token);
        }
    }
}