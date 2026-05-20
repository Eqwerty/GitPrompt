using FluentAssertions;
using GitPrompt.Constants;

namespace GitPrompt.Tests.Unit.Constants;

public sealed class PromptColorTests
{
    [Fact]
    public void Wrap_WhenCalled_ShouldSurroundTextWithColorAndReset()
    {
        // Arrange
        var color = new PromptColor("\u0001\e[32m\u0002");
        var reset = PromptColors.ColorReset;

        // Act
        var result = color.Wrap("hello");

        // Assert
        result.Should().Be($"\u0001\e[32m\u0002hello{reset}");
    }

    [Fact]
    public void Wrap_WhenTextIsEmpty_ShouldReturnColorAndReset()
    {
        // Arrange
        var color = new PromptColor("\u0001\e[31m\u0002");
        var reset = PromptColors.ColorReset;

        // Act
        var result = color.Wrap(string.Empty);

        // Assert
        result.Should().Be($"\u0001\e[31m\u0002{reset}");
    }

    [Fact]
    public void ImplicitConversion_WhenAssignedToString_ShouldReturnValue()
    {
        // Arrange
        var color = new PromptColor("\u0001\e[32m\u0002");

        // Act
        string result = color;

        // Assert
        result.Should().Be("\u0001\e[32m\u0002");
    }
}
