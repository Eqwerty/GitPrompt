using FluentAssertions;
using GitPrompt.Constants;

namespace GitPrompt.Tests.Unit.Constants;

[Collection(AnsiColorIsolationCollection.Name)]
public sealed class AnsiColorSupportTests
{
    [Fact]
    public void IsEnabled_WhenNoColorAndTermAreUnset_ShouldReturnTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NO_COLOR", null);
        Environment.SetEnvironmentVariable("TERM", null);

        // Act & Assert
        AnsiColorSupport.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WhenNoColorIsSet_ShouldReturnFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NO_COLOR", "1");
        Environment.SetEnvironmentVariable("TERM", null);

        try
        {
            // Act & Assert
            AnsiColorSupport.IsEnabled.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NO_COLOR", null);
        }
    }

    [Fact]
    public void IsEnabled_WhenNoColorIsEmptyString_ShouldReturnTrue()
    {
        // Arrange — per the NO_COLOR convention, only a non-empty value disables color
        Environment.SetEnvironmentVariable("NO_COLOR", string.Empty);
        Environment.SetEnvironmentVariable("TERM", null);

        try
        {
            // Act & Assert
            AnsiColorSupport.IsEnabled.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NO_COLOR", null);
        }
    }

    [Fact]
    public void IsEnabled_WhenTermIsDumb_ShouldReturnFalse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NO_COLOR", null);
        Environment.SetEnvironmentVariable("TERM", "dumb");

        try
        {
            // Act & Assert
            AnsiColorSupport.IsEnabled.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", null);
        }
    }

    [Fact]
    public void IsEnabled_WhenTermIsXterm_ShouldReturnTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("NO_COLOR", null);
        Environment.SetEnvironmentVariable("TERM", "xterm-256color");

        try
        {
            // Act & Assert
            AnsiColorSupport.IsEnabled.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", null);
        }
    }
}
