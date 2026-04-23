using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Prompting;

internal readonly record struct PromptResult(
    string ContextSegment,
    string GitStatusSegment,
    string PromptSymbol,
    TimeSpan ContextElapsed,
    TimeSpan GitElapsed,
    TimeSpan TotalElapsed)
{
    internal string Output =>
        $"{PromptLine}\n{ColorPromptSymbol}{PromptSymbol}{ColorReset} ";

    internal string PromptLine => string.IsNullOrEmpty(GitStatusSegment)
        ? ContextSegment
        : $"{ContextSegment} {GitStatusSegment}";
}
