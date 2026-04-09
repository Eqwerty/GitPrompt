using Prompt.Platform;

namespace Prompt.Prompting;

internal static class PromptSymbolBuilder
{
    internal static string Build(PlatformProvider platformProvider)
    {
        if (platformProvider.IsWindows())
        {
            return ">";
        }

        var isCurrentUnixRootUser = string.Equals(platformProvider.User, "root", StringComparison.Ordinal);
        return isCurrentUnixRootUser ? "#" : "$";
    }
}
