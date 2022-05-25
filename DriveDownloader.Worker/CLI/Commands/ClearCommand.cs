using System.Text;
using System.Text.RegularExpressions;

namespace DriveDownloader.Worker.CLI.Commands;

internal class ClearCommand : ICommand
{
    private readonly Regex _pattern = new("^clear$", RegexOptions.Compiled);
    public string CommandText => "clear";

    public string Execute(string command)
        => Enumerable.Range(0, 100).Aggregate("", (a, _) => a + Environment.NewLine);

    public bool CanExecute(string command)
        => _pattern.IsMatch(command);


    public string Explain()
        => new StringBuilder()
            .AppendLine("Usage => clear")
            .AppendLine("It clears the screen")
            .ToString();

}