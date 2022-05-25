using System.Text;
using System.Text.RegularExpressions;

namespace DriveDownloader.Worker.CLI.Commands;

internal class HelpCommand : ICommand
{
    private readonly Regex _pattern = new(@"^help(?<command>\s+.+)?$",RegexOptions.Compiled);
    public string CommandText => "help";

    public string Execute(string command)
    {
        var match = _pattern.Match(command);

        if (match.Groups["command"].Success)
        {
            var matchedCommand = InteractivePrompt.GetCommands().FirstOrDefault(t => t.CommandText == match.Groups["command"].Value.Trim());
            if (matchedCommand is not null)
                return matchedCommand.Explain();
        }
        
        return string.Join('\n', InteractivePrompt.GetCommands().Select(t => t.Explain()));
    }

    public bool CanExecute(string command)
        => _pattern.IsMatch(command);

    public string Explain()
        => new StringBuilder()
            .AppendLine("Usage => help")
            .AppendLine("It prints all the commands")
            .ToString();
}