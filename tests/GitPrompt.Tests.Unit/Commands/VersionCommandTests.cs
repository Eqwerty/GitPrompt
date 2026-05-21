using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class VersionCommandTests
{
    [Fact]
    public void PrintVersion_WhenCalled_ShouldWriteNonEmptyOutput()
    {
        // Arrange
        var output = new StringWriter();

        // Act
        VersionCommand.PrintVersion(output);

        // Assert
        output.ToString().Trim().Should().NotBeEmpty();
    }

    [Fact]
    public void PrintVersion_WhenInformationalVersionIsEmpty_ShouldWriteUnknown()
    {
        // Act & Assert
        VersionCommand.GetVersion(informationalVersion: "").Should().Be("unknown");
    }

    [Fact]
    public void PrintVersion_WhenInformationalVersionIsWhitespace_ShouldWriteUnknown()
    {
        // Act & Assert
        VersionCommand.GetVersion(informationalVersion: "   ").Should().Be("unknown");
    }

    [Fact]
    public void PrintVersion_WhenInformationalVersionIsProvided_ShouldReturnIt()
    {
        // Act & Assert
        VersionCommand.GetVersion(informationalVersion: "abc1234").Should().Be("abc1234");
    }
}
