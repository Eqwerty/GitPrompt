using FluentAssertions;
using GitPrompt.Tests.Unit.Git;

namespace GitPrompt.Tests.Unit;

public sealed class AtomicFileWriterTests
{
    [Fact]
    public void WriteAtomically_WhenGivenStringContent_ShouldWriteExactContent()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var targetPath = Path.Combine(directory.DirectoryPath, "config.jsonc");

        // Act
        AtomicFileWriter.WriteAtomically(targetPath, "{ \"a\": 1 }");

        // Assert
        File.ReadAllText(targetPath).Should().Be("{ \"a\": 1 }");
    }

    [Fact]
    public void WriteAtomically_WhenGivenLines_ShouldWriteExactLines()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var targetPath = Path.Combine(directory.DirectoryPath, "record.cache");

        // Act
        AtomicFileWriter.WriteAtomically(targetPath, ["first", "second", "third"]);

        // Assert
        File.ReadAllLines(targetPath).Should().Equal("first", "second", "third");
    }

    [Fact]
    public void WriteAtomically_WhenTargetFileAlreadyExists_ShouldOverwriteContent()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var targetPath = Path.Combine(directory.DirectoryPath, "config.jsonc");
        File.WriteAllText(targetPath, "old content");

        // Act
        AtomicFileWriter.WriteAtomically(targetPath, "new content");

        // Assert
        File.ReadAllText(targetPath).Should().Be("new content");
    }

    [Fact]
    public void WriteAtomically_WhenWriteSucceeds_ShouldNotLeaveTempFileBehind()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var targetPath = Path.Combine(directory.DirectoryPath, "config.jsonc");

        // Act
        AtomicFileWriter.WriteAtomically(targetPath, "content");

        // Assert
        Directory.GetFiles(directory.DirectoryPath).Should().Equal(targetPath);
    }
}
