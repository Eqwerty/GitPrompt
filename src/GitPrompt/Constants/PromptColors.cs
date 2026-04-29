namespace GitPrompt.Constants;

internal static class PromptColors
{
    private const string ReadLineStart = "\u0001";
    private const string ReadLineEnd = "\u0002";

    private static string PromptColor(string ansiColor) => $"{ReadLineStart}{ansiColor}{ReadLineEnd}";

    private static string HexColor(string hexColor) => PromptColor(AnsiColorConverter.ToAnsi(hexColor));

    internal static string ColorUser           => HexColor(AnsiColors.Green);
    internal static string ColorHost           => HexColor(AnsiColors.Magenta);
    internal static string ColorPath           => HexColor(AnsiColors.Orange);
    internal static string ColorCommandDuration => HexColor(AnsiColors.Magenta);
    internal static string ColorBranch         => HexColor(AnsiColors.Blue);
    internal static string ColorBranchNoUpstream => HexColor(AnsiColors.Blue);
    internal static string ColorAhead          => HexColor(AnsiColors.Blue);
    internal static string ColorBehind         => HexColor(AnsiColors.Blue);
    internal static string ColorStaged         => HexColor(AnsiColors.Green);
    internal static string ColorUnstaged       => HexColor(AnsiColors.Red);
    internal static string ColorUntracked      => HexColor(AnsiColors.Red);
    internal static string ColorStash          => HexColor(AnsiColors.Magenta);
    internal static string ColorConflict       => HexColor(AnsiColors.BoldRed);
    internal static string ColorMissingPath    => HexColor(AnsiColors.BoldRed);
    internal static string ColorTimeout        => HexColor(AnsiColors.Yellow);
    internal static string ColorPromptSymbol   => HexColor(AnsiColors.LightGray);
    internal static readonly string ColorReset  = PromptColor(AnsiColors.Reset);
}
