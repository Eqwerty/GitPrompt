using FluentAssertions;
using Prompt.Git;

namespace Prompt.Tests.Unit.Git;

public sealed class StatusCountsTests
{
    [Fact]
    public void StatusCounts_WhenCreatedWithNoArguments_ShouldInitializeAllCountsToZero()
    {
        // Arrange
        var statusCounts = new StatusCounts();

        // Assert
        statusCounts.StagedAdded.Should().Be(0);
        statusCounts.StagedModified.Should().Be(0);
        statusCounts.StagedDeleted.Should().Be(0);
        statusCounts.StagedRenamed.Should().Be(0);
        statusCounts.UnstagedAdded.Should().Be(0);
        statusCounts.UnstagedModified.Should().Be(0);
        statusCounts.UnstagedDeleted.Should().Be(0);
        statusCounts.UnstagedRenamed.Should().Be(0);
        statusCounts.Untracked.Should().Be(0);
        statusCounts.Conflicts.Should().Be(0);
    }

    [Fact]
    public void StatusCounts_WhenCreatedWithExplicitValues_ShouldExposeConstructorValues()
    {
        // Arrange
        var statusCounts = new StatusCounts(
            stagedAdded: 1,
            stagedModified: 2,
            stagedDeleted: 3,
            stagedRenamed: 4,
            unstagedAdded: 5,
            unstagedModified: 6,
            unstagedDeleted: 7,
            unstagedRenamed: 8,
            untracked: 9,
            conflicts: 10);

        // Assert
        statusCounts.StagedAdded.Should().Be(1);
        statusCounts.StagedModified.Should().Be(2);
        statusCounts.StagedDeleted.Should().Be(3);
        statusCounts.StagedRenamed.Should().Be(4);
        statusCounts.UnstagedAdded.Should().Be(5);
        statusCounts.UnstagedModified.Should().Be(6);
        statusCounts.UnstagedDeleted.Should().Be(7);
        statusCounts.UnstagedRenamed.Should().Be(8);
        statusCounts.Untracked.Should().Be(9);
        statusCounts.Conflicts.Should().Be(10);
    }

    [Theory]
    [InlineData('A', nameof(StatusCounts.StagedAdded))]
    [InlineData('M', nameof(StatusCounts.StagedModified))]
    [InlineData('D', nameof(StatusCounts.StagedDeleted))]
    [InlineData('R', nameof(StatusCounts.StagedRenamed))]
    [InlineData('C', nameof(StatusCounts.StagedRenamed))]
    public void TrackStagedStatusCode_WhenSupportedStatusCodeIsTracked_ShouldIncrementMatchingStagedCounter(char statusCode, string expectedCounterName)
    {
        // Arrange
        var statusCounts = new StatusCounts();

        // Act
        statusCounts.TrackStagedStatusCode(statusCode);

        // Assert
        AssertSingleCounterWasIncremented(statusCounts, expectedCounterName);
    }

    [Theory]
    [InlineData('A', nameof(StatusCounts.UnstagedAdded))]
    [InlineData('M', nameof(StatusCounts.UnstagedModified))]
    [InlineData('D', nameof(StatusCounts.UnstagedDeleted))]
    [InlineData('R', nameof(StatusCounts.UnstagedRenamed))]
    [InlineData('C', nameof(StatusCounts.UnstagedRenamed))]
    public void TrackUnstagedStatusCode_WhenSupportedStatusCodeIsTracked_ShouldIncrementMatchingUnstagedCounter(char statusCode, string expectedCounterName)
    {
        // Arrange
        var statusCounts = new StatusCounts();

        // Act
        statusCounts.TrackUnstagedStatusCode(statusCode);

        // Assert
        AssertSingleCounterWasIncremented(statusCounts, expectedCounterName);
    }

    [Fact]
    public void TrackUntrackedFile_WhenCalled_ShouldIncrementUntrackedCount()
    {
        // Arrange
        var statusCounts = new StatusCounts();

        // Act
        statusCounts.TrackUntrackedFile();

        // Assert
        AssertSingleCounterWasIncremented(statusCounts, nameof(StatusCounts.Untracked));
    }

    [Fact]
    public void TrackConflict_WhenCalled_ShouldIncrementConflictCount()
    {
        // Arrange
        var statusCounts = new StatusCounts();

        // Act
        statusCounts.TrackConflict();

        // Assert
        AssertSingleCounterWasIncremented(statusCounts, nameof(StatusCounts.Conflicts));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TrackStatusCode_WhenConflictCodeIsTracked_ShouldIncrementConflictCount(bool isStaged)
    {
        // Arrange
        var statusCounts = new StatusCounts();

        // Act
        if (isStaged)
        {
            statusCounts.TrackStagedStatusCode('U');
        }
        else
        {
            statusCounts.TrackUnstagedStatusCode('U');
        }

        // Assert
        AssertSingleCounterWasIncremented(statusCounts, nameof(StatusCounts.Conflicts));
    }

    [Theory]
    [InlineData('.', true)]
    [InlineData('.', false)]
    [InlineData(' ', true)]
    [InlineData(' ', false)]
    public void TrackStatusCode_WhenUnsupportedStatusCodeIsTracked_ShouldLeaveAllCountsUnchanged(char statusCode, bool isStaged)
    {
        // Arrange
        var statusCounts = new StatusCounts();

        // Act
        if (isStaged)
        {
            statusCounts.TrackStagedStatusCode(statusCode);
        }
        else
        {
            statusCounts.TrackUnstagedStatusCode(statusCode);
        }

        // Assert
        statusCounts.StagedAdded.Should().Be(0);
        statusCounts.StagedModified.Should().Be(0);
        statusCounts.StagedDeleted.Should().Be(0);
        statusCounts.StagedRenamed.Should().Be(0);
        statusCounts.UnstagedAdded.Should().Be(0);
        statusCounts.UnstagedModified.Should().Be(0);
        statusCounts.UnstagedDeleted.Should().Be(0);
        statusCounts.UnstagedRenamed.Should().Be(0);
        statusCounts.Untracked.Should().Be(0);
        statusCounts.Conflicts.Should().Be(0);
    }

    private static void AssertSingleCounterWasIncremented(StatusCounts statusCounts, string expectedCounterName)
    {
        statusCounts.StagedAdded.Should().Be(expectedCounterName == nameof(StatusCounts.StagedAdded) ? 1 : 0);
        statusCounts.StagedModified.Should().Be(expectedCounterName == nameof(StatusCounts.StagedModified) ? 1 : 0);
        statusCounts.StagedDeleted.Should().Be(expectedCounterName == nameof(StatusCounts.StagedDeleted) ? 1 : 0);
        statusCounts.StagedRenamed.Should().Be(expectedCounterName == nameof(StatusCounts.StagedRenamed) ? 1 : 0);
        statusCounts.UnstagedAdded.Should().Be(expectedCounterName == nameof(StatusCounts.UnstagedAdded) ? 1 : 0);
        statusCounts.UnstagedModified.Should().Be(expectedCounterName == nameof(StatusCounts.UnstagedModified) ? 1 : 0);
        statusCounts.UnstagedDeleted.Should().Be(expectedCounterName == nameof(StatusCounts.UnstagedDeleted) ? 1 : 0);
        statusCounts.UnstagedRenamed.Should().Be(expectedCounterName == nameof(StatusCounts.UnstagedRenamed) ? 1 : 0);
        statusCounts.Untracked.Should().Be(expectedCounterName == nameof(StatusCounts.Untracked) ? 1 : 0);
        statusCounts.Conflicts.Should().Be(expectedCounterName == nameof(StatusCounts.Conflicts) ? 1 : 0);
    }
}
