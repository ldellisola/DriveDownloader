using System.Text;
using System.Text.RegularExpressions;
using DriveDownloader.Worker.Models;
using Microsoft.EntityFrameworkCore;

namespace DriveDownloader.Worker.CLI.Commands;

internal class QuitCommand : ICommand 
{
    private readonly Regex _pattern = new ("^quit$", RegexOptions.Compiled);

    public string CommandText => "quit";

    public string Execute(string command)
    {
        InteractivePrompt.Exit();
        return "Shutting down...";
    }

    public bool CanExecute(string command)
        => _pattern.IsMatch(command);

    public string Explain()
        => new StringBuilder()
            .AppendLine("Usage => quit")
            .AppendLine("It will quit the application")
            .ToString();
}