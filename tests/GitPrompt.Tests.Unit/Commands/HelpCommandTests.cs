using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class HelpCommandTests
{
    public static TheoryData<string> VisibleCommandUsages => [..CommandRegistry.VisibleCommands.Select(command => command.Usage)];

    [Theory]
    [MemberData(nameof(VisibleCommandUsages))]
    public void PrintHelp_ShouldOutputEachVisibleCommandUsage(string usage)
    {
        // Arrange
        var output = new StringWriter();

        // Act
        HelpCommand.PrintHelp(output);

        // Assert
        output.ToString().Should().Contain(usage);
    }

    [Fact]
    public void PrintHelp_ShouldNotOutputHiddenCommands()
    {
        // Arrange
        var output = new StringWriter();
        var hiddenUsages = CommandRegistry.Commands
            .Where(command => command.IsHidden)
            .Select(command => command.Usage)
            .ToList();

        // Act
        HelpCommand.PrintHelp(output);

        // Assert
        var text = output.ToString();
        foreach (var usage in hiddenUsages)
        {
            text.Should().NotContain(usage);
        }
    }

    [Fact]
    public void PrintHelp_ShouldOutputAllVisibleDescriptions()
    {
        // Arrange
        var output = new StringWriter();

        // Act
        HelpCommand.PrintHelp(output);

        // Assert
        var text = output.ToString();
        foreach (var command in CommandRegistry.VisibleCommands)
        {
            text.Should().Contain(command.Description);
        }
    }

    [Fact]
    public void PrintHelp_ShouldAlignDescriptionsToTheSameColumn()
    {
        // Arrange
        var output = new StringWriter();
        var expectedPadWidth = CommandRegistry.VisibleCommands.Max(command => command.Usage.Length) + 5;
        var expectedDescriptionColumn = 2 + expectedPadWidth;

        // Act
        HelpCommand.PrintHelp(output);

        // Assert
        var commandLines = output.ToString()
            .Split('\n')
            .Where(line => line.StartsWith("  gitprompt"))
            .ToList();

        commandLines.Should().NotBeEmpty();
        commandLines.Should().OnlyContain(
            line => line.Length > expectedDescriptionColumn && !char.IsWhiteSpace(line[expectedDescriptionColumn]),
            $"all descriptions should start at column {expectedDescriptionColumn}");
    }

    [Theory]
    [InlineData("Flags:")]
    [InlineData("Setup:")]
    [InlineData("Configuration:")]
    [InlineData("Aliases:")]
    [InlineData("Maintenance:")]
    [InlineData("Diagnostics:")]
    public void PrintHelp_ShouldOutputEachSectionHeader(string header)
    {
        // Arrange
        var output = new StringWriter();

        // Act
        HelpCommand.PrintHelp(output);

        // Assert
        output.ToString().Should().Contain(header);
    }

    [Theory]
    [InlineData("Configuration:", "gitprompt config", "gitprompt config reset [-y]")]
    [InlineData("Aliases:", "gitprompt aliases", "gitprompt aliases enable", "gitprompt aliases disable")]
    [InlineData("Maintenance:", "gitprompt update", "gitprompt update aliases", "gitprompt uninstall")]
    [InlineData("Diagnostics:", "gitprompt debug", "gitprompt paths")]
    public void PrintHelp_WhenSectionHasSubCommands_ShouldGroupThemUnderSectionHeader(string header, params string[] usages)
    {
        // Arrange
        var output = new StringWriter();

        // Act
        HelpCommand.PrintHelp(output);

        // Assert
        var text = output.ToString();
        var headerIndex = text.IndexOf(header, StringComparison.Ordinal);
        headerIndex.Should().BeGreaterThan(-1, $"section '{header}' should be present");

        var nextHeaderIndex = FindNextSectionHeader(text, headerIndex + header.Length);

        foreach (var usage in usages)
        {
            var usageIndex = text.IndexOf(usage, StringComparison.Ordinal);
            usageIndex.Should().BeGreaterThan(headerIndex, $"'{usage}' should appear after '{header}'");
            if (nextHeaderIndex >= 0)
            {
                usageIndex.Should().BeLessThan(nextHeaderIndex, $"'{usage}' should appear before the next section");
            }
        }
    }

    private static int FindNextSectionHeader(string text, int startIndex)
    {
        var headers = new[] { "Flags:", "Setup:", "Configuration:", "Aliases:", "Maintenance:", "Diagnostics:" };
        var minIndex = -1;

        foreach (var header in headers)
        {
            var idx = text.IndexOf(header, startIndex, StringComparison.Ordinal);
            if (idx >= 0 && (minIndex < 0 || idx < minIndex))
            {
                minIndex = idx;
            }
        }

        return minIndex;
    }
}

