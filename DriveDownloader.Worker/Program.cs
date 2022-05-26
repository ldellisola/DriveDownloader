using DriveDownloader.Worker;
using DriveDownloader.Worker.CLI;
using DriveDownloader.Worker.CLI.Commands;
using SimpleGoogleDrive.Models;


var status = await DbHelpers.Init();

var settings = new DriveAuthSettings(
    status.AppName!,
    status.ClientId!,
    status.ClientSecret!,
    "Google.Drive.Tools.Uploader.Auth",
    mode: DriveAuthSettings.AuthMode.Console
);

using var drive = await new SimpleGoogleDrive.GoogleDriveService(
        settings, 
        true, 
        "/data/storage.json")
    .Authenticate();

ArgumentNullException.ThrowIfNull(drive);


await DbHelpers.RestoreDownloadingFiles();

TaskStorage.RunAndStore(c=> DbHelpers.Load(drive, status.RemoteBaseFolder!,c));

var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;
cancellationToken.ThrowIfCancellationRequested();

TaskStorage.RunAndStore(c => ThreadActions.ManageDownloadThreads(drive, c));

InteractivePrompt.AddCommand<QuitCommand>();
InteractivePrompt.AddCommand<StartCommand>();
InteractivePrompt.AddCommand<StopCommand>();
InteractivePrompt.AddCommands(new SetCommand(drive));
InteractivePrompt.AddCommand<GetCommand>();
InteractivePrompt.AddCommand<RetryCommand>();
InteractivePrompt.AddCommand<StatusCommand>();
InteractivePrompt.AddCommand<HelpCommand>();
InteractivePrompt.AddCommand<ClearCommand>();

InteractivePrompt.Run("drive: ");


try
{
    TaskStorage.CancelTasks();
    TaskStorage.WaitAll();
}
catch
{
    // ignored
}