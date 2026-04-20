using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class CommandRegistryTests
{
    [Fact]
    public void Commands_ShouldNotBeEmpty()
    {
        // Act & Assert
        CommandRegistry.Commands.Should().NotBeEmpty();
    }

    [Fact]
    public void Commands_ShouldHaveNonEmptyDescriptionForEveryEntry()
    {
        // Act & Assert
        CommandRegistry.Commands.Should().OnlyContain(command => !string.IsNullOrWhiteSpace(command.Description));
    }

    [Fact]
    public void Commands_ShouldHaveAtLeastOneVerbForEveryEntry()
    {
        // Act & Assert
        CommandRegistry.Commands.Should().OnlyContain(command =>
            command.Verbs.Length > 0 && command.Verbs.All(v => !string.IsNullOrWhiteSpace(v)));
    }

    [Fact]
    public void Commands_ShouldNotHaveDuplicateVerbs()
    {
        // Arrange
        var allVerbs = CommandRegistry.Commands.SelectMany(command => command.Verbs).ToList();

        // Act & Assert
        allVerbs.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void TryGetCommandByVerb_WhenKnownVerb_ShouldReturnTrue()
    {
        // Arrange
        var allVerbs = CommandRegistry.Commands.SelectMany(command => command.Verbs).ToList();

        // Act & Assert
        foreach (var verb in allVerbs)
        {
            CommandRegistry.TryGetCommandByVerb(verb, out _).Should().BeTrue(because: $"'{verb}' is a registered verb");
        }
    }

    [Fact]
    public void TryGetCommandByVerb_WhenUnknownVerb_ShouldReturnFalse()
    {
        // Act & Assert
        CommandRegistry.TryGetCommandByVerb("unknown-verb", out _).Should().BeFalse();
    }
}
