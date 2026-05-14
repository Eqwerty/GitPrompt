using FluentAssertions;
using GitPrompt.Git;

namespace GitPrompt.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class GitStatusMergeOperationIntegrationTests
{
    [Fact]
    public async Task BuildGitStatusSegment_WhenNoUpstreamBranchIsMerging_ShouldShowMergeOperationInsideBranchLabel()
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

        // Act
        var mergeCommandResult = await TestHelpers.RunGitAllowFailureAsync(repositoryPath, "merge other");
        var gitStatusSegment = GitStatusSegmentBuilder.Build(repositoryPath);

        // Assert
        mergeCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain(TestHelpers.BranchLabelWithOperation(TestHelpers.TrackedBranchLabel("feature"), "MERGE"));
        gitStatusSegment.Should().NotContain(TestHelpers.NoUpstreamBranchLabel("feature"));
    }
}
