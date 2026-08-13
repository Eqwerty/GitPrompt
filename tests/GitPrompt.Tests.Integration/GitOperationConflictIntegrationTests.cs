using FluentAssertions;
using GitPrompt.Constants;
using GitPrompt.Git;

namespace GitPrompt.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class GitOperationConflictIntegrationTests
{
    [Fact]
    public async Task BuildGitStatusSegment_WhenMergeBasedRebaseConflicts_ShouldShowRebaseProgressAndConflictCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add conflict.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature 1\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"feature change 1\"");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature 2\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"feature change 2\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main change\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"main change\"");
        await TestHelpers.RunGitAsync(repositoryPath, "checkout feature");

        // Act — plain `git rebase` uses the merge backend (rebase-merge/) by default on this git version
        var rebaseResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, "rebase main");
        var gitStatusSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        rebaseResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain(TestHelpers.BranchLabelWithOperation(TestHelpers.TrackedBranchLabel("feature"), "REBASE 1/2"));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconConflicts, 1));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenApplyBasedRebaseConflicts_ShouldShowRebaseProgressAndConflictCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add conflict.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature 1\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"feature change 1\"");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature 2\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"feature change 2\"");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature 3\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"feature change 3\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main change\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"main change\"");
        await TestHelpers.RunGitAsync(repositoryPath, "checkout feature");

        // Act — `--apply` forces the legacy patch-based backend (rebase-apply/)
        var rebaseResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, "rebase --apply main");
        var gitStatusSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        rebaseResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain(TestHelpers.BranchLabelWithOperation(TestHelpers.TrackedBranchLabel("feature"), "REBASE 1/3"));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconConflicts, 1));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenRebasePausesWithoutConflict_ShouldShowRebaseProgressWithoutConflictCount()
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
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "f1.txt"), "f1\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add f1.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"f1\"");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "f2.txt"), "f2\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add f2.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"f2\"");

        // Act — `--exec` with a command that always fails pauses the rebase after replaying a
        // commit, with no file conflict at all (each of the 2 commits contributes a pick step and
        // an exec step to the todo list, so it stops at step 2 of 4, right after the first pick).
        // HEAD is technically detached during any rebase, but GitOperationDetector.ResolveRebaseBranchName
        // resolves the original branch name from rebase-merge/head-name, so it still renders as a
        // normal named branch label rather than a detached-commit label.
        var rebaseResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, "rebase --exec \"exit 1\" main");
        var gitStatusSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        rebaseResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain(TestHelpers.BranchLabelWithOperation(TestHelpers.TrackedBranchLabel("feature"), "REBASE 2/4"));
        gitStatusSegment.Should().NotContain(PromptIcons.IconConflicts.ToString());
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenMergeConflictIsAborted_ShouldNoLongerShowMergeOperation()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add conflict.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"feature change\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b other main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "other\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"other change\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout feature");

        var mergeResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, "merge other");
        var duringMergeSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Act
        await TestHelpers.RunGitAsync(repositoryPath, "merge --abort");
        var afterAbortSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        mergeResult.ExitCode.Should().NotBe(0);
        duringMergeSegment.Should().Contain(TestHelpers.BranchLabelWithOperation(TestHelpers.TrackedBranchLabel("feature"), "MERGE"));
        afterAbortSegment.Should().Contain(TestHelpers.NoUpstreamBranchLabel("feature"));
        afterAbortSegment.Should().NotContain("MERGE");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenCherryPickSequenceConflicts_ShouldShowCherryPickOperationAndConflictCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "shared.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add shared.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, "checkout -b topic");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "shared.txt"), "topic1\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"topic1\"");
        var firstTopicCommit = (await TestHelpers.RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "other.txt"), "unrelated\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add other.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"unrelated\"");
        var secondTopicCommit = (await TestHelpers.RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        await TestHelpers.RunGitAsync(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "shared.txt"), "main change\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"main change\"");

        // Act — cherry-pick a sequence where the first commit conflicts
        var cherryPickResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, $"cherry-pick {firstTopicCommit} {secondTopicCommit}");
        var gitStatusSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        cherryPickResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain(TestHelpers.BranchLabelWithOperation(TestHelpers.TrackedBranchLabel("main"), "CHERRY-PICK"));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconConflicts, 1));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenRevertConflicts_ShouldShowRevertOperationAndConflictCount()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "shared.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add shared.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "shared.txt"), "changed\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"change\"");
        var changeCommit = (await TestHelpers.RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "shared.txt"), "changed again\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"change again\"");

        // Act — reverting the middle commit conflicts with the commit on top of it
        var revertResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, $"revert --no-edit {changeCommit}");
        var gitStatusSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        revertResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain(TestHelpers.BranchLabelWithOperation(TestHelpers.TrackedBranchLabel("main"), "REVERT"));
        gitStatusSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconConflicts, 1));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenBisectInProgressThenReset_ShouldShowBisectOperationThenReturnToNormal()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        // Bisect needs at least a couple of revisions strictly between "good" and "bad" to have
        // an actual midpoint to check out — with adjacent commits it has nothing to test and never
        // moves off the branch tip at all, which would defeat the point of this test.
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "a.txt"), "1\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add a.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"c1\"");
        var goodCommit = (await TestHelpers.RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "a.txt"), "2\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"c2\"");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "a.txt"), "3\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"c3\"");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "a.txt"), "4\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"c4\"");
        var badCommit = (await TestHelpers.RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();

        // Act
        await TestHelpers.RunGitAsync(repositoryPath, $"bisect start {badCommit} {goodCommit}");
        var bisectCommit = (await TestHelpers.RunGitAsync(repositoryPath, "rev-parse HEAD")).Trim();
        var duringBisectSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        await TestHelpers.RunGitAsync(repositoryPath, "bisect reset");
        var afterResetSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        duringBisectSegment.Should().Contain(
            TestHelpers.BranchLabelWithOperation(TestHelpers.DetachedBranchLabel($"{bisectCommit[..7]}..."), "BISECT"));
        afterResetSegment.Should().NotContain("BISECT");
        afterResetSegment.Should().Contain(TestHelpers.NoUpstreamBranchLabel("main"));
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenRebaseConflictsInLinkedWorktree_ShouldShowWorktreeBranchWithRebaseProgress()
    {
        // Arrange
        using var sandbox = new TestHelpers.TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");
        var worktreePath = Path.Combine(sandbox.DirectoryPath, "feature-worktree");

        await TestHelpers.RunGitAsync(sandbox.DirectoryPath, $"init --initial-branch=main {TestHelpers.Quote(repositoryPath)}");
        await TestHelpers.ConfigureGitIdentityAsync(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        await TestHelpers.RunGitAsync(repositoryPath, "add conflict.txt");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -m \"base\"");

        await TestHelpers.RunGitAsync(repositoryPath, $"worktree add -b feature {TestHelpers.Quote(worktreePath)}");
        await File.WriteAllTextAsync(Path.Combine(worktreePath, "conflict.txt"), "feature\n");
        await TestHelpers.RunGitAsync(worktreePath, "commit -am \"feature change\"");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main change\n");
        await TestHelpers.RunGitAsync(repositoryPath, "commit -am \"main change\"");

        // Act — rebase inside the linked worktree, not the main worktree
        var rebaseResult = await TestHelpers.RunGitAllowFailureAsync(worktreePath, "rebase main");
        var worktreeSegment = GitStatusSegmentBuilder.Build(worktreePath);
        var mainSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        rebaseResult.ExitCode.Should().NotBe(0);
        worktreeSegment.Should().Contain(TestHelpers.BranchLabelWithOperation(TestHelpers.TrackedBranchLabel("feature"), "REBASE 1/1"));
        worktreeSegment.Should().Contain(TestHelpers.Indicator(PromptIcons.IconConflicts, 1));
        mainSegment.Should().NotContain("REBASE");
    }
}
