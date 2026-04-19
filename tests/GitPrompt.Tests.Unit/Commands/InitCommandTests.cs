using FluentAssertions;
using GitPrompt.Commands;

namespace GitPrompt.Tests.Unit.Commands;

public sealed class InitCommandTests
{
    [Fact]
    public void GetShellError_ReturnsNull_ForBash()
    {
        // Act & Assert
        InitCommand.GetShellError("bash").Should().BeNull();
    }

    [Fact]
    public void GetShellError_ReturnsNull_ForBashCaseInsensitive()
    {
        // Act & Assert
        InitCommand.GetShellError("BASH").Should().BeNull();
    }

    [Fact]
    public void GetShellError_ReturnsError_WhenShellIsEmpty()
    {
        // Act
        var error = InitCommand.GetShellError(string.Empty);

        // Assert
        error.Should().Be("gitprompt: init requires a shell name");
    }

    [Fact]
    public void GetShellError_ReturnsError_WhenShellIsUnsupported()
    {
        // Act
        var error = InitCommand.GetShellError("zsh");

        // Assert
        error.Should().Be("gitprompt: unsupported shell for 'init': 'zsh'");
    }
}
