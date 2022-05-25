using DriveDownloader.Worker;
using DriveDownloader.Worker.CLI;
using DriveDownloader.Worker.CLI.Commands;
using SimpleGoogleDrive.Models;


var status = await DBHelpers.Init();

var settings = new DriveAuthSettings(
    status.AppName!,
    status.ClientId!,
    status.ClientSecret!,
    "Google.Drive.Tools.Uploader.Auth",
    mode: DriveAuthSettings.AuthMode.Console
);

using var drive = new SimpleGoogleDrive.GoogleDriveService(settings,true,"/data/storage.json");
await drive.Authenticate();


await DBHelpers.RestoreDownloadingFiles();
await DBHelpers.Load(drive, status.RemoteBaseFolder!);

var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;
cancellationToken.ThrowIfCancellationRequested();

var tasks = new List<Task> {
    ThreadActions.ManageDownloadThreads(drive, cancellationToken)};

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

cancellationTokenSource.Cancel();

try
{
    Task.WaitAll(tasks.ToArray(), cancellationToken);
}
catch
{
    // ignored
}