using GitPrompt.Configuration;

namespace GitPrompt.Constants;

internal static class PromptColors
{
    private const string ReadLineStart = "\u0001";
    private const string ReadLineEnd = "\u0002";

    private static string FormatAnsi(string ansiColor)
    {
        return $"{ReadLineStart}{ansiColor}{ReadLineEnd}";
    }

    private static PromptColor ResolveColor(string configColor, string defaultColor)
    {
        var color = configColor ?? defaultColor;

        try
        {
            return new PromptColor(FormatAnsi(AnsiColorConverter.ToAnsi(color)));
        }
        catch (ArgumentException)
        {
            return new PromptColor(FormatAnsi(AnsiColorConverter.ToAnsi(defaultColor)));
        }
    }

    internal static PromptColor ColorUser => ResolveColor(ConfigReader.Config.Colors.User, AnsiColors.Green);

    internal static PromptColor ColorHost => ResolveColor(ConfigReader.Config.Colors.Host, AnsiColors.Magenta);

    internal static PromptColor ColorPath => ResolveColor(ConfigReader.Config.Colors.Path, AnsiColors.Yellow);

    internal static PromptColor ColorCommandDuration => ResolveColor(ConfigReader.Config.Colors.CommandDuration, AnsiColors.Magenta);

    internal static PromptColor ColorBranch => ResolveColor(ConfigReader.Config.Colors.Branch, AnsiColors.BoldCyan);

    internal static PromptColor ColorBranchNoUpstream => ResolveColor(ConfigReader.Config.Colors.BranchNoUpstream, AnsiColors.Cyan);

    internal static PromptColor ColorBranchGoneUpstream => ResolveColor(ConfigReader.Config.Colors.BranchGoneUpstream, AnsiColors.Cyan);

    internal static PromptColor ColorBranchDetached => ResolveColor(ConfigReader.Config.Colors.BranchDetached, AnsiColors.DarkGray);

    internal static PromptColor ColorAhead => ResolveColor(ConfigReader.Config.Colors.Ahead, AnsiColors.BoldCyan);

    internal static PromptColor ColorBehind => ResolveColor(ConfigReader.Config.Colors.Behind, AnsiColors.BoldCyan);

    internal static PromptColor ColorStaged => ResolveColor(ConfigReader.Config.Colors.Staged, AnsiColors.Green);

    internal static PromptColor ColorUnstaged => ResolveColor(ConfigReader.Config.Colors.Unstaged, AnsiColors.Red);

    internal static PromptColor ColorUntracked => ResolveColor(ConfigReader.Config.Colors.Untracked, AnsiColors.Red);

    internal static PromptColor ColorStash => ResolveColor(ConfigReader.Config.Colors.Stash, AnsiColors.BrightBlack);

    internal static PromptColor ColorConflict => ResolveColor(ConfigReader.Config.Colors.Conflict, AnsiColors.Red);

    internal static PromptColor ColorDirty => ResolveColor(ConfigReader.Config.Colors.Dirty, AnsiColors.Yellow);

    internal static PromptColor ColorDirtyStaged => ResolveColor(ConfigReader.Config.Colors.DirtyStaged, AnsiColors.Green);

    internal static PromptColor ColorClean => ResolveColor(ConfigReader.Config.Colors.Clean, AnsiColors.Green);

    internal static PromptColor ColorMissingPath => ResolveColor(ConfigReader.Config.Colors.MissingPath, AnsiColors.Red);

    internal static PromptColor ColorTimeout => ResolveColor(ConfigReader.Config.Colors.Timeout, AnsiColors.Yellow);

    internal static PromptColor ColorPromptSymbol => ResolveColor(ConfigReader.Config.Colors.PromptSymbol, AnsiColors.White);

    internal static PromptColor ColorPrefix => ResolveColor(ConfigReader.Config.Colors.Prefix, AnsiColors.White);

    internal static readonly string ColorReset = FormatAnsi(AnsiColors.Reset);
}
