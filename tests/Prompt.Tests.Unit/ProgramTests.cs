using FluentAssertions;

namespace Prompt.Tests.Unit;

public sealed class ProgramTests
{
    [Fact]
    public void ParseGitStatusOutput_WhenStatusContainsAheadBehindAndCounters_ShouldParseSnapshotValues()
    {
        // Arrange
        const string statusOutput = """
                                    # branch.oid 1234567890abcdef1234567890abcdef12345678
                                    # branch.head master
                                    # branch.upstream origin/master
                                    # branch.ab +3 -2
                                    1 A. ignored
                                    1 .M ignored
                                    2 R. ignored
                                    2 .R ignored
                                    1 D. ignored
                                    1 .D ignored
                                    ? untracked.txt
                                    u UU ignored
                                    """;

        // Act
        var gitStatusSnapshot = Program.ParseGitStatusOutput(statusOutput);

        // Assert
        gitStatusSnapshot.BranchHeadName.Should().Be("master");
        gitStatusSnapshot.HeadObjectId.Should().Be("1234567890abcdef1234567890abcdef12345678");
        gitStatusSnapshot.UpstreamReference.Should().Be("origin/master");
        gitStatusSnapshot.HasUpstream.Should().BeTrue();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeTrue();
        gitStatusSnapshot.CommitsAhead.Should().Be(3);
        gitStatusSnapshot.CommitsBehind.Should().Be(2);

        var statusCounts = gitStatusSnapshot.StatusCounts;
        statusCounts.StagedAdded.Should().Be(1);
        statusCounts.UnstagedModified.Should().Be(1);
        statusCounts.StagedRenamed.Should().Be(1);
        statusCounts.UnstagedRenamed.Should().Be(1);
        statusCounts.StagedDeleted.Should().Be(1);
        statusCounts.UnstagedDeleted.Should().Be(1);
        statusCounts.Untracked.Should().Be(1);
        statusCounts.Conflicts.Should().Be(1);
    }

    [Fact]
    public void BuildGitStatusDisplay_WhenCountsAndOperationExist_ShouldIncludeReadableIndicators()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var stashLogDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "logs", "refs");
        Directory.CreateDirectory(stashLogDirectoryPath);
        File.WriteAllText(Path.Combine(stashLogDirectoryPath, "stash"), "entry-1\nentry-2\n");
        File.WriteAllText(Path.Combine(gitDirectory.DirectoryPath, "MERGE_HEAD"), "merge\n");

        var statusCounts = new StatusCounts(
            stagedRenamed: 1,
            unstagedModified: 1,
            untracked: 1,
            conflicts: 1);

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("(main)", 4, 2, statusCounts, gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain("(main|MERGE)");
        gitStatusDisplay.Should().Contain("↑4");
        gitStatusDisplay.Should().Contain("↓2");
        gitStatusDisplay.Should().Contain("~1");
        gitStatusDisplay.Should().Contain("→1");
        gitStatusDisplay.Should().Contain("?1");
        gitStatusDisplay.Should().Contain("@2");
        gitStatusDisplay.Should().Contain("!1");
    }

    [Theory]
    [InlineData("MERGE_HEAD", "MERGE")]
    [InlineData("CHERRY_PICK_HEAD", "CHERRY-PICK")]
    public void BuildGitStatusDisplay_WhenNoUpstreamBranchHasOperation_ShouldPlaceOperationInsideBranchLabel(
        string operationMarkerFileName,
        string expectedOperationMarker)
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(gitDirectory.DirectoryPath, operationMarkerFileName), "head\n");

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("*(feature)", 0, 0, new StatusCounts(), gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain($"*(feature|{expectedOperationMarker})");
    }

    [Fact]
    public void ResolveRebaseBranchName_WhenRebaseHeadNameFileExists_ShouldReturnBranchName()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var rebaseDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "rebase-merge");
        Directory.CreateDirectory(rebaseDirectoryPath);
        File.WriteAllText(Path.Combine(rebaseDirectoryPath, "head-name"), "refs/heads/feature\n");

        // Act
        var rebaseBranchName = Program.ResolveRebaseBranchName(gitDirectory.DirectoryPath);

        // Assert
        rebaseBranchName.Should().Be("feature");
    }

    [Fact]
    public void FindMatchingRemoteReferences_WhenLooseAndPackedRefsContainMatches_ShouldReturnMatchingReferences()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var remoteDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "refs", "remotes", "origin");
        Directory.CreateDirectory(remoteDirectoryPath);

        File.WriteAllText(Path.Combine(remoteDirectoryPath, "main"), "abcdef1234567890\n");
        File.WriteAllText(
            Path.Combine(gitDirectory.DirectoryPath, "packed-refs"),
            """
            # pack-refs with: peeled fully-peeled sorted
            abcdef1234567890 refs/remotes/origin/release
            1111111111111111 refs/remotes/origin/other
            """
        );

        // Act
        var matchingRemoteReferences = Program.FindMatchingRemoteReferences(gitDirectory.DirectoryPath, "abcdef1234567890");

        // Assert
        matchingRemoteReferences.Should().Contain("origin/main");
        matchingRemoteReferences.Should().Contain("origin/release");
        matchingRemoteReferences.Should().NotContain("origin/other");
    }

    [Fact]
    public void ReadStashEntryCount_WhenStashLogContainsEntries_ShouldCountStashLines()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        var stashLogDirectoryPath = Path.Combine(gitDirectory.DirectoryPath, "logs", "refs");
        Directory.CreateDirectory(stashLogDirectoryPath);
        File.WriteAllText(Path.Combine(stashLogDirectoryPath, "stash"), "first\nsecond\nthird\n");

        // Act
        var stashEntryCount = Program.ReadStashEntryCount(gitDirectory.DirectoryPath);

        // Assert
        stashEntryCount.Should().Be(3);
    }

    [Theory]
    [InlineData("", "\"\"")]
    [InlineData("simple", "simple")]
    [InlineData("two words", "\"two words\"")]
    public void EscapeCommandLineArgument_WhenInputVaries_ShouldQuoteOnlyWhenNecessary(string value, string expected)
    {
        // Arrange
        // Inline data provides the test inputs.

        // Act
        var escapedValue = Program.EscapeCommandLineArgument(value);

        // Assert
        escapedValue.Should().Be(expected);
    }

    [Fact]
    public void EscapeCommandLineArgument_WhenInputContainsBackslashesAndQuotes_ShouldEscapeCharactersInsideQuotedArgument()
    {
        // Arrange
        const string value = "C:\\Program Files\\My \"App\"";

        // Act
        var escapedValue = Program.EscapeCommandLineArgument(value);

        // Assert
        escapedValue.Should().Be("\"C:\\\\Program Files\\\\My \\\"App\\\"\"");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("123456", "123456")]
    [InlineData("1234567", "1234567")]
    [InlineData("1234567890", "1234567")]
    public void ShortenObjectId_WhenInputVaries_ShouldReturnExpectedShortForm(string objectId, string expectedShortObjectId)
    {
        // Arrange
        // Inline data provides the test inputs.

        // Act
        var shortObjectId = Program.ShortenObjectId(objectId);

        // Assert
        shortObjectId.Should().Be(expectedShortObjectId);
    }

    [Fact]
    public void ParseGitStatusOutput_WhenStatusContainsAllSupportedCodesWithoutUpstream_ShouldTrackCountsAndNoUpstreamState()
    {
        // Arrange
        const string statusOutput = """
                                    # branch.oid abcdef1234567890abcdef1234567890abcdef12
                                    # branch.head feature
                                    1 AM file-a
                                    1 MD file-b
                                    2 R. file-c file-c-renamed
                                    2 .R file-d file-d-renamed
                                    1 .A file-e
                                    1 .D file-f
                                    ? untracked.txt
                                    u UU conflict.txt
                                    """;

        // Act
        var gitStatusSnapshot = Program.ParseGitStatusOutput(statusOutput);

        // Assert
        gitStatusSnapshot.HasUpstream.Should().BeFalse();
        gitStatusSnapshot.HasAheadBehindCounts.Should().BeFalse();
        gitStatusSnapshot.CommitsAhead.Should().Be(0);
        gitStatusSnapshot.CommitsBehind.Should().Be(0);

        var statusCounts = gitStatusSnapshot.StatusCounts;
        statusCounts.StagedAdded.Should().Be(1);
        statusCounts.StagedModified.Should().Be(1);
        statusCounts.StagedDeleted.Should().Be(0);
        statusCounts.StagedRenamed.Should().Be(1);
        statusCounts.UnstagedAdded.Should().Be(1);
        statusCounts.UnstagedModified.Should().Be(1);
        statusCounts.UnstagedDeleted.Should().Be(2);
        statusCounts.UnstagedRenamed.Should().Be(1);
        statusCounts.Untracked.Should().Be(1);
        statusCounts.Conflicts.Should().Be(1);
    }

    [Fact]
    public void ReadGitOperationMarker_WhenNoOperationMarkerExists_ShouldReturnEmpty()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        // Act
        var operationMarker = Program.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().BeEmpty();
    }

    [Fact]
    public void ReadGitOperationMarker_WhenCherryPickHeadExists_ShouldReturnCherryPick()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(gitDirectory.DirectoryPath, "CHERRY_PICK_HEAD"), "head\n");

        // Act
        var operationMarker = Program.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().Be("CHERRY-PICK");
    }

    [Fact]
    public void ReadGitOperationMarker_WhenRevertHeadExists_ShouldReturnRevert()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(gitDirectory.DirectoryPath, "REVERT_HEAD"), "head\n");

        // Act
        var operationMarker = Program.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().Be("REVERT");
    }

    [Fact]
    public void ReadGitOperationMarker_WhenBisectLogExists_ShouldReturnBisect()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(gitDirectory.DirectoryPath, "BISECT_LOG"), "bisect\n");

        // Act
        var operationMarker = Program.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().Be("BISECT");
    }

    [Fact]
    public void ReadGitOperationMarker_WhenRebaseAndOtherMarkersExist_ShouldPrioritizeRebase()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(gitDirectory.DirectoryPath, "rebase-merge"));
        File.WriteAllText(Path.Combine(gitDirectory.DirectoryPath, "MERGE_HEAD"), "head\n");
        File.WriteAllText(Path.Combine(gitDirectory.DirectoryPath, "CHERRY_PICK_HEAD"), "head\n");

        // Act
        var operationMarker = Program.ReadGitOperationMarker(gitDirectory.DirectoryPath);

        // Assert
        operationMarker.Should().Be("REBASE");
    }

    [Fact]
    public void ResolveGitDirectoryPath_WhenDotGitPathIsDirectory_ShouldReturnDotGitDirectoryPath()
    {
        // Arrange
        using var repoDirectory = new TemporaryDirectory();
        var dotGitPath = Path.Combine(repoDirectory.DirectoryPath, ".git");
        Directory.CreateDirectory(dotGitPath);

        // Act
        var resolvedGitDirectoryPath = Program.ResolveGitDirectoryPath(dotGitPath);

        // Assert
        resolvedGitDirectoryPath.Should().Be(dotGitPath);
    }

    [Fact]
    public void ResolveGitDirectoryPath_WhenGitdirFileContainsRelativePath_ShouldResolveAbsoluteGitDirectoryPath()
    {
        // Arrange
        using var rootDirectory = new TemporaryDirectory();
        var actualGitDirectoryPath = Path.Combine(rootDirectory.DirectoryPath, "actual-git");
        var workingTreePath = Path.Combine(rootDirectory.DirectoryPath, "worktree");
        Directory.CreateDirectory(actualGitDirectoryPath);
        Directory.CreateDirectory(workingTreePath);

        var dotGitPath = Path.Combine(workingTreePath, ".git");
        File.WriteAllText(dotGitPath, "gitdir: ../actual-git\n");

        // Act
        var resolvedGitDirectoryPath = Program.ResolveGitDirectoryPath(dotGitPath);

        // Assert
        resolvedGitDirectoryPath.Should().Be(Path.GetFullPath(actualGitDirectoryPath));
    }

    [Fact]
    public void BuildGitStatusDisplay_WhenTrackedBranchLabelIsRendered_ShouldUseTrackedBranchColor()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("(main)", 0, 0, new StatusCounts(), gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().StartWith("\u0001\e[1;36m\u0002(main)\u0001\e[0m\u0002");
    }

    [Fact]
    public void BuildGitStatusDisplay_WhenNoUpstreamBranchLabelIsRendered_ShouldUseNoUpstreamBranchColor()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("*(feature)", 0, 0, new StatusCounts(), gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().StartWith("\u0001\e[1;36m\u0002*(feature)\u0001\e[0m\u0002");
    }

    [Fact]
    public void BuildGitStatusDisplay_WhenAheadBehindCountsAreRendered_ShouldUseAheadAndBehindColors()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("(main)", 2, 3, new StatusCounts(), gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain(" \u0001\e[1;36m\u0002↑2\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[1;36m\u0002↓3\u0001\e[0m\u0002");
    }

    [Fact]
    public void BuildGitStatusDisplay_WhenMultipleSegmentsAreRendered_ShouldResetColorAfterEachSegment()
    {
        // Arrange
        using var gitDirectory = new TemporaryDirectory();

        var statusCounts = new StatusCounts(
            stagedAdded: 1,
            unstagedModified: 1,
            untracked: 1,
            conflicts: 1);

        // Act
        var gitStatusDisplay = Program.BuildGitStatusDisplay("(main)", 1, 1, statusCounts, gitDirectory.DirectoryPath);

        // Assert
        gitStatusDisplay.Should().Contain("\u0001\e[1;36m\u0002(main)\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[1;36m\u0002↑1\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[1;36m\u0002↓1\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[0;32m\u0002+1\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[0;31m\u0002~1\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[0;31m\u0002?1\u0001\e[0m\u0002");
        gitStatusDisplay.Should().Contain(" \u0001\e[1;31m\u0002!1\u0001\e[0m\u0002");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "Prompt.Tests.Unit", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                foreach (var filePath in Directory.EnumerateFiles(DirectoryPath, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                }

                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
