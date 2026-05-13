using FluentAssertions;
using GitPrompt.Constants;

namespace GitPrompt.Tests.Unit.Constants;

public sealed class AnsiColorConverterTests
{
    [Fact]
    public void ToAnsi_WhenValidHex_ShouldReturnCorrect24BitAnsiCode()
    {
        // Act & Assert
        AnsiColorConverter.ToAnsi("#48A8CD").Should().Be("\e[38;2;72;168;205m");
    }

    [Fact]
    public void ToAnsi_WhenHexIsBlack_ShouldReturnZeroComponents()
    {
        // Act & Assert
        AnsiColorConverter.ToAnsi("#000000").Should().Be("\e[38;2;0;0;0m");
    }

    [Fact]
    public void ToAnsi_WhenHexIsWhite_ShouldReturnMaxComponents()
    {
        // Act & Assert
        AnsiColorConverter.ToAnsi("#FFFFFF").Should().Be("\e[38;2;255;255;255m");
    }

    [Theory]
    [InlineData("#00BB00", "\e[38;2;0;187;0m")]
    [InlineData("#CB06B2", "\e[38;2;203;6;178m")]
    [InlineData("#FFA002", "\e[38;2;255;160;2m")]
    [InlineData("#CC0000", "\e[38;2;204;0;0m")]
    [InlineData("#FF5555", "\e[38;2;255;85;85m")]
    [InlineData("#AAAAAA", "\e[38;2;170;170;170m")]
    public void ToAnsi_WhenKnownHexColors_ShouldReturnExpectedAnsiCode(string hex, string expected)
    {
        // Act & Assert
        AnsiColorConverter.ToAnsi(hex).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#GGGGGG")]
    public void ToAnsi_WhenHexIsInvalid_ShouldThrowArgumentException(string hex)
    {
        // Act
        var act = () => AnsiColorConverter.ToAnsi(hex);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToAnsi_WhenHexIsNull_ShouldThrow()
    {
        // Act
        var act = () => AnsiColorConverter.ToAnsi(null!);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("32", "\e[32m")]
    [InlineData("[32m", "\e[32m")]
    [InlineData("32m", "\e[32m")]
    [InlineData("[32", "\e[32m")]
    [InlineData("\e[32m", "\e[32m")]
    [InlineData("\e[32", "\e[32m")]
    [InlineData("\\e[32m", "\e[32m")]
    [InlineData("\\e[32", "\e[32m")]
    public void ToAnsi_WhenAnsiCodeVariants_ShouldNormalizeToFullEscapeSequence(string input, string expected)
    {
        // Act & Assert
        AnsiColorConverter.ToAnsi(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("1;33", "\e[1;33m")]
    [InlineData("[1;33m", "\e[1;33m")]
    [InlineData("1;33m", "\e[1;33m")]
    [InlineData("[1;33", "\e[1;33m")]
    [InlineData("\e[1;33m", "\e[1;33m")]
    [InlineData("\e[1;33", "\e[1;33m")]
    [InlineData("\\e[1;33m", "\e[1;33m")]
    [InlineData("0;36", "\e[0;36m")]
    [InlineData("38;5;208", "\e[38;5;208m")]
    public void ToAnsi_WhenCompoundAnsiCodes_ShouldNormalizeToFullEscapeSequence(string input, string expected)
    {
        // Act & Assert
        AnsiColorConverter.ToAnsi(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("green")]
    [InlineData("bold")]
    [InlineData("[abc]")]
    [InlineData("\e[]")]
    public void ToAnsi_WhenAnsiCodeIsInvalid_ShouldThrowArgumentException(string input)
    {
        // Act
        var act = () => AnsiColorConverter.ToAnsi(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
