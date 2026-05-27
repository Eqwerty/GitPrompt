using GitPrompt.Platform;

namespace GitPrompt.Commands;

internal static class HelpCommand
{
    internal static void PrintHelp(TextWriter? output = null)
    {
        output ??= Console.Out;

        var visibleCommands = CommandRegistry.VisibleCommands;
        var padWidth = visibleCommands.Max(command => command.Usage.Length) + 5;

        var groups = visibleCommands
            .GroupBy(command => command.Group ?? string.Empty)
            .ToList();

        output.WriteLine("GitPrompt - fast Git prompt for Bash");

        foreach (var group in groups)
        {
            output.WriteLine();
            output.WriteLine($"{group.Key}:");

            foreach (var command in group)
            {
                output.WriteLine($"  {command.Usage.PadRight(padWidth)}{command.Description}");
            }
        }
    }
}
