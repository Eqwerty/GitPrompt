using FluentAssertions;
using GitPrompt.Git;

namespace GitPrompt.Tests.Unit.Git;

public sealed class GitStatusCountsTests
{
    [Fact]
    public void IsDirty_WhenAllCountsAreZero_ShouldReturnFalse()
    {
        // Arrange & Act
        var counts = new GitStatusCounts();

        // Assert
        counts.IsDirty.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(0, 1, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 1, 0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 1, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 0, 1, 0, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 0, 0, 1, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 1, 0, 0)]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 1, 0)]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
    public void IsDirty_WhenAnyCountIsNonZero_ShouldReturnTrue(
        int stagedAdded,
        int stagedModified,
        int stagedDeleted,
        int stagedRenamed,
        int unstagedAdded,
        int unstagedModified,
        int unstagedDeleted,
        int unstagedRenamed,
        int untracked,
        int conflicts)
    {
        // Arrange & Act
        var counts = new GitStatusCounts(
            stagedAdded,
            stagedModified,
            stagedDeleted,
            stagedRenamed,
            unstagedAdded,
            unstagedModified,
            unstagedDeleted,
            unstagedRenamed,
            untracked,
            conflicts);

        // Assert
        counts.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void IsDirtyStaged_WhenAllCountsAreZero_ShouldReturnFalse()
    {
        // Arrange & Act
        var counts = new GitStatusCounts();

        // Assert
        counts.IsDirtyStaged.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 0, 0, 0)]
    [InlineData(0, 1, 0, 0)]
    [InlineData(0, 0, 1, 0)]
    [InlineData(0, 0, 0, 1)]
    public void IsDirtyStaged_WhenOnlyStagedChangesExist_ShouldReturnTrue(
        int stagedAdded,
        int stagedModified,
        int stagedDeleted,
        int stagedRenamed)
    {
        // Arrange & Act
        var counts = new GitStatusCounts(
            StagedAdded: stagedAdded,
            StagedModified: stagedModified,
            StagedDeleted: stagedDeleted,
            StagedRenamed: stagedRenamed);

        // Assert
        counts.IsDirtyStaged.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 0, 1, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 0, 1)]
    [InlineData(1, 0, 0, 0, 1, 0)]
    [InlineData(0, 0, 0, 1, 0, 0)]
    [InlineData(0, 0, 0, 0, 1, 0)]
    [InlineData(0, 0, 0, 0, 0, 1)]
    public void IsDirtyStaged_WhenUnstagedOrOtherChangesExist_ShouldReturnFalse(
        int stagedAdded,
        int unstagedAdded,
        int unstagedModified,
        int unstagedDeleted,
        int untracked,
        int conflicts)
    {
        // Arrange & Act
        var counts = new GitStatusCounts(
            StagedAdded: stagedAdded,
            UnstagedAdded: unstagedAdded,
            UnstagedModified: unstagedModified,
            UnstagedDeleted: unstagedDeleted,
            Untracked: untracked,
            Conflicts: conflicts);

        // Assert
        counts.IsDirtyStaged.Should().BeFalse();
    }
}
