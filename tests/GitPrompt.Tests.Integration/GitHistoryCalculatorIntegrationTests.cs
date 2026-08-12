using FluentAssertions;
using GitPrompt.Git;

namespace GitPrompt.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class GitHistoryCalculatorIntegrationTests
{
    [Fact]
    public async Task ComputeLocalAheadCommitCount_WhenOriginHeadSymrefResolves_UsesRemoteDefaultBranch()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();

        var remoteRepositoryPath = Path.Combine(sandbox.DirectoryPath, "remote.git");
        var seedRepositoryPath = Path.Combine(sandbox.DirectoryPath, "seed");
        var localRepositoryPath = Path.Combine(sandbox.DirectoryPath, "local");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --bare --initial-branch=main {TestHelpers.Quote(remoteRepositoryPath)}");

        // Seed the remote's default branch before the real clone below, so that clone
        // can auto-resolve refs/remotes/origin/HEAD (an empty remote has no branch to point at yet).
        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(seedRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(seedRepositoryPath);
        await File.WriteAllTextAsync(Path.Combine(seedRepositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(seedRepositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(seedRepositoryPath, "commit -m \"base\"");
        await TestHelpers.RunGitAsync(seedRepositoryPath, "push -u origin main");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(localRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(localRepositoryPath);

        await TestHelpers.RunGitAsync(localRepositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "feature-1.txt"), "1\n");
        await TestHelpers.RunGitAsync(localRepositoryPath, "add feature-1.txt");
        await TestHelpers.RunGitAsync(localRepositoryPath, "commit -m \"feature 1\"");
        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "feature-2.txt"), "2\n");
        await TestHelpers.RunGitAsync(localRepositoryPath, "add feature-2.txt");
        await TestHelpers.RunGitAsync(localRepositoryPath, "commit -m \"feature 2\"");

        // Sanity check: a plain clone of a repo whose default branch was pushed to
        // should auto-resolve refs/remotes/origin/HEAD.
        var originHead = await TestHelpers.RunGitAsync(localRepositoryPath, "symbolic-ref refs/remotes/origin/HEAD");
        originHead.Trim().Should().Be("refs/remotes/origin/main");

        // Act
        var aheadCount = GitHistoryCalculator.ComputeLocalAheadCommitCount(localRepositoryPath, "feature");

        // Assert
        aheadCount.Should().Be(2);
    }

    [Fact]
    public async Task ComputeLocalAheadCommitCount_WhenOriginHeadMissingButUpstreamConfigured_UsesUpstreamReference()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();

        var remoteRepositoryPath = Path.Combine(sandbox.DirectoryPath, "remote.git");
        var seedRepositoryPath = Path.Combine(sandbox.DirectoryPath, "seed");
        var localRepositoryPath = Path.Combine(sandbox.DirectoryPath, "local");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --bare --initial-branch=main {TestHelpers.Quote(remoteRepositoryPath)}");

        // Seed the remote's default branch before the real clone below, so that clone
        // can auto-resolve refs/remotes/origin/HEAD (an empty remote has no branch to point at yet).
        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(seedRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(seedRepositoryPath);
        await File.WriteAllTextAsync(Path.Combine(seedRepositoryPath, "base.txt"), "base\n");
        await TestHelpers.RunGitAsync(seedRepositoryPath, "add base.txt");
        await TestHelpers.RunGitAsync(seedRepositoryPath, "commit -m \"base\"");
        await TestHelpers.RunGitAsync(seedRepositoryPath, "push -u origin main");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"clone {TestHelpers.Quote(remoteRepositoryPath)} {TestHelpers.Quote(localRepositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(localRepositoryPath);

        await TestHelpers.RunGitAsync(localRepositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "feature-1.txt"), "1\n");
        await TestHelpers.RunGitAsync(localRepositoryPath, "add feature-1.txt");
        await TestHelpers.RunGitAsync(localRepositoryPath, "commit -m \"feature 1\"");
        await TestHelpers.RunGitAsync(localRepositoryPath, "push -u origin feature");

        // Local advances beyond what was pushed, so origin/feature (the upstream) is behind.
        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "feature-2.txt"), "2\n");
        await TestHelpers.RunGitAsync(localRepositoryPath, "add feature-2.txt");
        await TestHelpers.RunGitAsync(localRepositoryPath, "commit -m \"feature 2\"");
        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "feature-3.txt"), "3\n");
        await TestHelpers.RunGitAsync(localRepositoryPath, "add feature-3.txt");
        await TestHelpers.RunGitAsync(localRepositoryPath, "commit -m \"feature 3\"");

        // Remove origin/HEAD so ResolveBaseReference must fall through to @{u}.
        await TestHelpers.RunGitAsync(localRepositoryPath, "symbolic-ref -d refs/remotes/origin/HEAD");

        // Act
        var aheadCount = GitHistoryCalculator.ComputeLocalAheadCommitCount(localRepositoryPath, "feature");

        // Assert — ahead of origin/feature (its upstream), not origin/main
        aheadCount.Should().Be(2);
    }

    [Fact]
    public async Task ComputeLocalAheadCommitCount_WhenNoRemoteButLocalMainExists_FallsBackToLocalMainCandidate()
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
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "feature-1.txt"), "1\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add feature-1.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"feature 1\"");

        // Act
        var aheadCount = GitHistoryCalculator.ComputeLocalAheadCommitCount(repositoryPath, "feature");

        // Assert — no remote at all, but a local "main" branch exists as a candidate base
        aheadCount.Should().Be(1);
    }

    [Fact]
    public async Task ComputeLocalAheadCommitCount_WhenNoCandidateBranchesExist_FallsBackToTotalCommitCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();

        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        // Neither "main" nor "master" exists anywhere in this repo, and there's no remote.
        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=trunk {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "one.txt"), "1\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add one.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"one\"");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "two.txt"), "2\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add two.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"two\"");

        // Act
        var aheadCount = GitHistoryCalculator.ComputeLocalAheadCommitCount(repositoryPath, "trunk");

        // Assert — every fallback exhausted, so the total commit count on HEAD is reported
        aheadCount.Should().Be(2);
    }

    [Fact]
    public async Task ComputeLocalAheadCommitCount_WhenCurrentBranchIsMainItself_SkipsComparingAgainstItself()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();

        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "one.txt"), "1\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add one.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"one\"");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "two.txt"), "2\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add two.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"two\"");

        // Act — currently on "main" itself, with no remote and no other candidate branch
        var aheadCount = GitHistoryCalculator.ComputeLocalAheadCommitCount(repositoryPath, "main");

        // Assert — "main"/"origin/main" candidates are skipped as self-matches, so this
        // falls all the way through to the total-commit-count fallback rather than
        // comparing main against itself (which would wrongly report 0).
        aheadCount.Should().Be(2);
    }
}
