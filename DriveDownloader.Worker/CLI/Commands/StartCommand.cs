using System.Text;
using System.Text.RegularExpressions;
using DriveDownloader.Worker.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveDownloader.Worker.CLI.Commands;

internal class StartCommand : ICommand
{
    private readonly Regex _pattern = new ("^start$", RegexOptions.Compiled);

    public string CommandText => "start";

    public string Execute(string command)
    {
        using var db = new DriveDBContext();
        var status = db.Status.FirstOrDefault() ?? new Status();
        
        status.IsRunning = true;
        
        db.Status.Update(status);
        db.SaveChanges();
        return "Service starting up";
    }

    public bool CanExecute(string command)
        => _pattern.IsMatch(command);

    public string Explain() =>
        new StringBuilder()
            .AppendLine("Usage => start")
            .AppendLine("It will start the service")
            .ToString();
}