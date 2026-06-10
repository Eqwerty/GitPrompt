using FluentAssertions;
using GitPrompt.Constants;
using GitPrompt.Git;

namespace GitPrompt.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class GitStatusBranchOperationIntegrationTests
{
    [Fact]
    public async Task BuildGitStatusSegment_WhenTrackedBranchHasLocalAndRemoteCommits_ShouldShowBranchAndAheadBehindCounts()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var remoteRepositoryPath = Path.Combine(sandbox.DirectoryPath, "remote.git");
        var sourceRepositoryPath = Path.Combine(sandbox.DirectoryPath, "source");
        var localRepositoryPath = Path.Combine(sandbox.DirectoryPath, "local");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --bare --initial-branch=main {TestHelpers.Quote(remoteRepositoryPath)}");
        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(sourceRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(sourceRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "commit -m \"base\"");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "push -u origin main");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(localRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(localRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "local-ahead.txt"), "ahead\n");
        await TestHelpers.RunGitAsync(localRepositoryPath, "add local-ahead.txt");
        await TestHelpers.RunGitAsync(localRepositoryPath, "commit -m \"local ahead\"");

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "remote-ahead.txt"), "behind\n");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "add remote-ahead.txt");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "commit -m \"remote ahead\"");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "push");
        await TestHelpers.RunGitAsync(localRepositoryPath, "fetch origin");

        // Act
        var gitStatusSegment = GitStatusSegmentBuilder.Build(localRepositoryPath);

        // Assert
        gitStatusSegment.Should().Contain(TestHelpers.TrackedBranchLabel("main"));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconAhead, 1));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconBehind, 1));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenBranchHasNoUpstreamAndLocalCommits_ShouldShowNoUpstreamMarkerAndAheadCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");
        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b feature");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "feature.txt"), "feature\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add feature.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"feature commit\"");

        // Act
        var gitStatusSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        gitStatusSegment.Should().Contain(TestHelpers.NoUpstreamBranchLabel("feature"));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconAhead, 1));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenHeadIsDetached_ShouldShowCheckedOutCommit()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "commit-a.txt"), "a\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add commit-a.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"commit a\"");
        var commitAObjectId = (await TestHelpers.RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "commit-b.txt"), "b\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add commit-b.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"commit b\"");

        await TestHelpers.RunGitAsync(repositoryPath, $"checkout --detach {commitAObjectId}");

        // Act
        var gitStatusSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        gitStatusSegment.Should().Contain($"[{commitAObjectId[..7]}...]");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenDetachedHeadMatchesSingleRemoteReference_ShouldShowRemoteReferenceAndShortCommit()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var remoteRepositoryPath = Path.Combine(sandbox.DirectoryPath, "remote.git");
        var sourceRepositoryPath = Path.Combine(sandbox.DirectoryPath, "source");
        var localRepositoryPath = Path.Combine(sandbox.DirectoryPath, "local");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --bare --initial-branch=main {TestHelpers.Quote(remoteRepositoryPath)}");
        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(sourceRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(sourceRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "commit -m \"base\"");
        var commitObjectId = (await TestHelpers.RunGitAsync(sourceRepositoryPath, "rev-parse HEAD")).Trim();
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "push -u origin main");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(localRepositoryPath)}");
        await TestHelpers.RunGitAsync(localRepositoryPath, $"checkout --detach {commitObjectId}");

        // Act
        var gitStatusSegment = GitStatusSegmentBuilder.Build(localRepositoryPath);

        // Assert
        gitStatusSegment.Should().Contain($"[origin/main {commitObjectId[..7]}...]");
    }

    [Fact]
    public async Task ComputeLocalAheadCommitCount_WhenOriginHeadIsMissingButUpstreamExists_ShouldUseUpstreamFallback()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var remoteRepositoryPath = Path.Combine(sandbox.DirectoryPath, "remote.git");
        var sourceRepositoryPath = Path.Combine(sandbox.DirectoryPath, "source");
        var localRepositoryPath = Path.Combine(sandbox.DirectoryPath, "local");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --bare --initial-branch=main {TestHelpers.Quote(remoteRepositoryPath)}");
        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(sourceRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(sourceRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "commit -m \"base\"");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "push -u origin main");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(localRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(localRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "local-ahead.txt"), "ahead\n");
        await TestHelpers.RunGitAsync(localRepositoryPath, "add local-ahead.txt");
        await TestHelpers.RunGitAsync(localRepositoryPath, "commit -m \"local ahead\"");

        var upstreamReference = (await TestHelpers.RunGitAsync(localRepositoryPath, "rev-parse --abbrev-ref --symbolic-full-name @{u}")).Trim();
        upstreamReference.Should().Be("origin/main");

        await TestHelpers.RunGitAsync(localRepositoryPath, "symbolic-ref -d refs/remotes/origin/HEAD");
        var originHeadResult = await TestHelpers.RunGitAllowFailureAsync(localRepositoryPath, "symbolic-ref --quiet --short refs/remotes/origin/HEAD");
        originHeadResult.ExitCode.Should().NotBe(0);

        // Act
        var localAheadCount = GitHistoryCalculator.ComputeLocalAheadCommitCount(localRepositoryPath, "main");

        // Assert
        localAheadCount.Should().Be(1);
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenFreshRepoOnDefaultBranchWithCommitsAndNoRemote_ShouldShowAheadCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "a.txt"), "a\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add a.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"first\"");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "b.txt"), "b\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add b.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"second\"");

        // Act
        var gitStatusSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        gitStatusSegment.Should().Contain(TestHelpers.NoUpstreamBranchLabel("main"));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconAhead, 2));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenFreshRepoOnNonDefaultBranchWithCommitsAndNoRemote_ShouldShowAheadCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=feature {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "a.txt"), "a\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add a.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"first\"");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "b.txt"), "b\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add b.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"second\"");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "c.txt"), "c\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add c.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"third\"");

        // Act
        var gitStatusSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        gitStatusSegment.Should().Contain(TestHelpers.NoUpstreamBranchLabel("feature"));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconAhead, 3));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenRemoteBranchIsDeletedButTrackingConfigRemains_ShouldShowTrackedBranchLabelAndAheadCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var remoteRepositoryPath = Path.Combine(sandbox.DirectoryPath, "remote.git");
        var sourceRepositoryPath = Path.Combine(sandbox.DirectoryPath, "source");
        var localRepositoryPath = Path.Combine(sandbox.DirectoryPath, "local");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --bare --initial-branch=main {TestHelpers.Quote(remoteRepositoryPath)}");
        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(sourceRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(sourceRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "commit -m \"base\"");
        await TestHelpers.RunGitAsync(sourceRepositoryPath, "push -u origin main");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(localRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(localRepositoryPath);

        // Create and push the feature branch so the tracking config is established
        await TestHelpers.RunGitAsync(localRepositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "feature.txt"), "feature\n");
        await TestHelpers.RunGitAsync(localRepositoryPath, "add feature.txt");
        await TestHelpers.RunGitAsync(localRepositoryPath, "commit -m \"feature commit\"");
        await TestHelpers.RunGitAsync(localRepositoryPath, "push -u origin feature");

        // Add a local commit so there is something ahead
        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "feature2.txt"), "feature2\n");
        await TestHelpers.RunGitAsync(localRepositoryPath, "add feature2.txt");
        await TestHelpers.RunGitAsync(localRepositoryPath, "commit -m \"feature commit 2\"");

        // Delete the remote branch — tracking config stays, remote tracking ref is gone
        await TestHelpers.RunGitAsync(localRepositoryPath, "push origin :feature");

        // Act
        var gitStatusSegment = GitStatusSegmentBuilder.Build(localRepositoryPath);

        // Assert
        gitStatusSegment.Should().Contain(TestHelpers.TrackedBranchLabel("feature"));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconAhead, 2));
    }
}
