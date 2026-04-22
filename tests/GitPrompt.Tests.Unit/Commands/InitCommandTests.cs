using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class InitCommandTests
{
    [Fact]
    public void GenerateBashInit_ShouldNotIncludeMultiWordVerbsInTopLevelCompletions()
    {
        // Arrange
        var multiWordVerbs = CommandRegistry.VisibleCommands
            .Select(command => command.Verb)
            .Where(verb => verb.Contains(' '))
            .ToList();

        // Act
        var script = InitCommand.GenerateBashInit();

        // Assert
        var topLevelCompletionLine = script
            .Split('\n')
            .Single(line => line.TrimStart().StartsWith("gitprompt|gitprompt.exe)"));

        foreach (var verb in multiWordVerbs)
        {
            topLevelCompletionLine.Should().NotContain(verb,
                because: $"multi-word verb '{verb}' must not appear in top-level completions");
        }
    }

    [Fact]
    public void GenerateBashInit_ShouldIncludeTopLevelVerbsInCompletions()
    {
        // Arrange
        var expectedVerbs = CommandRegistry.VisibleCommands
            .Select(command => command.Verb)
            .Where(verb => !verb.Contains(' '))
            .ToList();

        // Act
        var script = InitCommand.GenerateBashInit();

        // Assert
        var topLevelCompletionLine = script
            .Split('\n')
            .Single(line => line.TrimStart().StartsWith("gitprompt|gitprompt.exe)"));

        foreach (var verb in expectedVerbs)
        {
            topLevelCompletionLine.Should().Contain(verb,
                because: $"top-level verb '{verb}' should appear in completions");
        }
    }
}
