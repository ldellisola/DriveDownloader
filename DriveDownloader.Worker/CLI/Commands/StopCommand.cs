using System.Text;
using System.Text.RegularExpressions;
using DriveDownloader.Worker.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveDownloader.Worker.CLI.Commands;

internal class StopCommand: ICommand
{
    private readonly Regex _pattern = new ("^stop$", RegexOptions.Compiled);
    public string CommandText => "stop";

    public string Execute(string command)
    {
        using var db = new DriveDBContext();
        var status = db.Status.FirstOrDefault() ?? new Status();
        
        status.IsRunning = false;
        
        db.Status.Update(status);
        db.SaveChanges();
        return "Service stopping...";
    }

    public bool CanExecute(string command)
        => _pattern.IsMatch(command);

    public string Explain() =>
        new StringBuilder()
            .AppendLine("Usage => stop")
            .AppendLine("It will start the service")
            .ToString();

}