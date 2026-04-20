using GitPrompt.Commands;

namespace GitPrompt;

internal static class ArgumentProcessor
{
    internal static void HandleArguments(string[] arguments)
    {
        if (arguments.Length is 0)
        {
            return;
        }

        if (!CommandRegistry.TryGetCommandByVerb(arguments[0], out var command))
        {
            Console.Error.WriteLine($"gitprompt: '{arguments[0]}' is not a gitprompt command. See gitprompt --help.");
            Environment.Exit(1);
        }

        command.Execute(arguments);

        Environment.Exit(0);
    }
}
