using System.Diagnostics;
using GitPrompt.Git;
using GitPrompt.Platform;

namespace GitPrompt.Prompting;

internal static class PromptBuilder
{
    internal static PromptResult Build(PlatformProvider platformProvider)
    {
        var workingDirectoryPath = platformProvider.WorkingDirectory.Path;

        var totalSw = Stopwatch.StartNew();

        var contextSw = Stopwatch.StartNew();
        var contextSegment = ContextSegmentBuilder.Build(platformProvider);
        contextSw.Stop();

        var gitSw = Stopwatch.StartNew();
        var gitStatusSegment = GitStatusSegmentBuilder.Build(workingDirectoryPath);
        gitSw.Stop();

        var promptSymbol = PromptSymbolBuilder.Build(platformProvider);
        totalSw.Stop();

        return new PromptResult(
            contextSegment,
            gitStatusSegment,
            promptSymbol,
            contextSw.Elapsed,
            gitSw.Elapsed,
            totalSw.Elapsed);
    }
}
