namespace DriveDownloader.Worker.CLI.Commands;

public interface ICommand
{
    public string CommandText { get; }
    public string Execute(string command);
    public bool CanExecute(string command);
    public string Explain();
}