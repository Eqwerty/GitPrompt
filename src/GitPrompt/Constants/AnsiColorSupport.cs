namespace GitPrompt.Constants;

internal static class AnsiColorSupport
{
    internal static bool IsEnabled => !HasNoColorSet() && !IsDumbTerminal();

    private static bool HasNoColorSet()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
    }

    private static bool IsDumbTerminal()
    {
        return Environment.GetEnvironmentVariable("TERM") is "dumb";
    }
}
