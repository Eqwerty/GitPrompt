using FluentAssertions;
using GitPrompt.Constants;

namespace GitPrompt.Tests.Unit.Constants;

[Collection(AnsiColorIsolationCollection.Name)]
public sealed class PromptColorsTests
{
    [Fact]
    public void ColorUser_WhenNoColorIsSet_ShouldBeEmpty()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NO_COLOR", "1");

        try
        {
            // Act & Assert
            ((string)PromptColors.ColorUser).Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NO_COLOR", null);
        }
    }

    [Fact]
    public void ColorReset_WhenNoColorIsSet_ShouldBeEmpty()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NO_COLOR", "1");

        try
        {
            // Act & Assert
            PromptColors.ColorReset.Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NO_COLOR", null);
        }
    }

    [Fact]
    public void ColorReset_WhenTermIsDumb_ShouldBeEmpty()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TERM", "dumb");

        try
        {
            // Act & Assert
            PromptColors.ColorReset.Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", null);
        }
    }
}
