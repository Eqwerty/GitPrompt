using FluentAssertions;
using GitPrompt.Git;

namespace GitPrompt.Tests.Unit.Git;

public sealed class UtilitiesTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("123456", "123456")]
    [InlineData("1234567", "1234567")]
    [InlineData("1234567890", "1234567")]
    public void ShortenCommitHash_WhenInputVaries_ShouldReturnShortForm(string objectId, string expectedShortHash)
    {
        // Act
        var shortHash = Utilities.ShortenCommitHash(objectId);

        // Assert
        shortHash.Should().Be(expectedShortHash);
    }

    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("simple", "simple")]
    [InlineData("two words", "\"two words\"")]
    public void EscapeCommandLineArgument_WhenInputVaries_ShouldQuoteOnlyWhenRequired(string argument, string expected)
    {
        // Act
        var escapedValue = Utilities.EscapeCommandLineArgument(argument);

        // Assert
        escapedValue.Should().Be(expected);
    }

    [Fact]
    public void EscapeCommandLineArgument_WhenInputContainsBackslashesAndQuotes_ShouldEscapeCharactersInsideQuotedValue()
    {
        // Arrange
        const string argument = "C:\\Program Files\\My \"App\"";

        // Act
        var escapedValue = Utilities.EscapeCommandLineArgument(argument);

        // Assert
        escapedValue.Should().Be("\"C:\\\\Program Files\\\\My \\\"App\\\"\"");
    }
}
