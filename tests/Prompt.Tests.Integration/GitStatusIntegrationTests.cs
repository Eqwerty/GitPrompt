using System.Diagnostics;
using FluentAssertions;

namespace Prompt.Tests.Integration;

[Collection("GitStatusSerialTests")]
public sealed class GitStatusIntegrationTests
{
    [Fact]
    public async Task BuildGitStatusSegment_WhenTrackedBranchHasLocalAndRemoteCommits_ShouldShowBranchAndAheadBehindCounts()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var remoteRepositoryPath = Path.Combine(sandbox.DirectoryPath, "remote.git");
        var sourceRepositoryPath = Path.Combine(sandbox.DirectoryPath, "source");
        var localRepositoryPath = Path.Combine(sandbox.DirectoryPath, "local");

        RunGit(sandbox.DirectoryPath, $"init --bare --initial-branch=main {Quote(remoteRepositoryPath)}");
        RunGit(sandbox.DirectoryPath, $"clone {Quote(remoteRepositoryPath)} {Quote(sourceRepositoryPath)}");
        ConfigureGitIdentity(sourceRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "base.txt"), "base\n");
        RunGit(sourceRepositoryPath, "add base.txt");
        RunGit(sourceRepositoryPath, "commit -m \"base\"");
        RunGit(sourceRepositoryPath, "push -u origin main");

        RunGit(sandbox.DirectoryPath, $"clone {Quote(remoteRepositoryPath)} {Quote(localRepositoryPath)}");
        ConfigureGitIdentity(localRepositoryPath);

        await File.WriteAllTextAsync(Path.Combine(localRepositoryPath, "local-ahead.txt"), "ahead\n");
        RunGit(localRepositoryPath, "add local-ahead.txt");
        RunGit(localRepositoryPath, "commit -m \"local ahead\"");

        await File.WriteAllTextAsync(Path.Combine(sourceRepositoryPath, "remote-ahead.txt"), "behind\n");
        RunGit(sourceRepositoryPath, "add remote-ahead.txt");
        RunGit(sourceRepositoryPath, "commit -m \"remote ahead\"");
        RunGit(sourceRepositoryPath, "push");
        RunGit(localRepositoryPath, "fetch origin");

        // Act
        var gitStatusSegment = await ExecuteInDirectory(localRepositoryPath, Program.BuildGitStatusSegmentAsync);

        // Assert
        gitStatusSegment.Should().Contain("(main)");
        gitStatusSegment.Should().Contain("↑1");
        gitStatusSegment.Should().Contain("↓1");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenBranchHasNoUpstreamAndLocalCommits_ShouldShowNoUpstreamMarkerAndAheadCount()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        RunGit(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        ConfigureGitIdentity(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "base.txt"), "base\n");
        RunGit(repositoryPath, "add base.txt");
        RunGit(repositoryPath, "commit -m \"base\"");
        RunGit(repositoryPath, "checkout -b feature");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "feature.txt"), "feature\n");
        RunGit(repositoryPath, "add feature.txt");
        RunGit(repositoryPath, "commit -m \"feature commit\"");

        // Act
        var gitStatusSegment = await ExecuteInDirectory(repositoryPath, Program.BuildGitStatusSegmentAsync);

        // Assert
        gitStatusSegment.Should().Contain("*(feature)");
        gitStatusSegment.Should().Contain("↑1");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenHeadIsDetached_ShouldShowCheckedOutCommit()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        RunGit(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        ConfigureGitIdentity(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "commit-a.txt"), "a\n");
        RunGit(repositoryPath, "add commit-a.txt");
        RunGit(repositoryPath, "commit -m \"commit a\"");
        var commitAObjectId = RunGit(repositoryPath, "rev-parse HEAD").Trim();

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "commit-b.txt"), "b\n");
        RunGit(repositoryPath, "add commit-b.txt");
        RunGit(repositoryPath, "commit -m \"commit b\"");

        RunGit(repositoryPath, $"checkout --detach {commitAObjectId}");

        // Act
        var gitStatusSegment = await ExecuteInDirectory(repositoryPath, Program.BuildGitStatusSegmentAsync);

        // Assert
        gitStatusSegment.Should().Contain($"({commitAObjectId[..7]}...)");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenStashExists_ShouldShowStashMarker()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        RunGit(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        ConfigureGitIdentity(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "tracked.txt"), "base\n");
        RunGit(repositoryPath, "add tracked.txt");
        RunGit(repositoryPath, "commit -m \"base\"");

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "tracked.txt"), "changed\n");
        RunGit(repositoryPath, "stash push -m \"wip\"");

        // Act
        var gitStatusSegment = await ExecuteInDirectory(repositoryPath, Program.BuildGitStatusSegmentAsync);

        // Assert
        gitStatusSegment.Should().Contain("@1");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenMergeIsInProgress_ShouldShowMergeOperationMarker()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        RunGit(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        ConfigureGitIdentity(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        RunGit(repositoryPath, "add conflict.txt");
        RunGit(repositoryPath, "commit -m \"base\"");

        RunGit(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        RunGit(repositoryPath, "commit -am \"feature change\"");

        RunGit(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main\n");
        RunGit(repositoryPath, "commit -am \"main change\"");

        // Act
        var mergeCommandResult = RunGitAllowFailure(repositoryPath, "merge feature");
        var gitStatusSegment = await ExecuteInDirectory(repositoryPath, Program.BuildGitStatusSegmentAsync);

        // Assert
        mergeCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("|MERGE");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenCherryPickIsInProgress_ShouldShowCherryPickOperationMarker()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        RunGit(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        ConfigureGitIdentity(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        RunGit(repositoryPath, "add conflict.txt");
        RunGit(repositoryPath, "commit -m \"base\"");

        RunGit(repositoryPath, "checkout -b source");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "source\n");
        RunGit(repositoryPath, "commit -am \"source change\"");
        var sourceCommitObjectId = RunGit(repositoryPath, "rev-parse HEAD").Trim();

        RunGit(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main\n");
        RunGit(repositoryPath, "commit -am \"main change\"");

        // Act
        var cherryPickCommandResult = RunGitAllowFailure(repositoryPath, $"cherry-pick {sourceCommitObjectId}");
        var gitStatusSegment = await ExecuteInDirectory(repositoryPath, Program.BuildGitStatusSegmentAsync);

        // Assert
        cherryPickCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("|CHERRY-PICK");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenNoUpstreamBranchIsMerging_ShouldShowMergeOperationInsideBranchLabel()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        RunGit(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        ConfigureGitIdentity(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        RunGit(repositoryPath, "add conflict.txt");
        RunGit(repositoryPath, "commit -m \"base\"");

        RunGit(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        RunGit(repositoryPath, "commit -am \"feature change\"");

        RunGit(repositoryPath, "checkout -b other main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "other\n");
        RunGit(repositoryPath, "commit -am \"other change\"");

        RunGit(repositoryPath, "checkout feature");

        // Act
        var mergeCommandResult = RunGitAllowFailure(repositoryPath, "merge other");
        var gitStatusSegment = await ExecuteInDirectory(repositoryPath, Program.BuildGitStatusSegmentAsync);

        // Assert
        mergeCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("*(feature|MERGE)");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenNoUpstreamBranchIsCherryPicking_ShouldShowCherryPickOperationInsideBranchLabel()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        RunGit(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        ConfigureGitIdentity(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        RunGit(repositoryPath, "add conflict.txt");
        RunGit(repositoryPath, "commit -m \"base\"");

        RunGit(repositoryPath, "checkout -b source");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "source\n");
        RunGit(repositoryPath, "commit -am \"source change\"");
        var sourceCommitObjectId = RunGit(repositoryPath, "rev-parse HEAD").Trim();

        RunGit(repositoryPath, "checkout -b feature main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        RunGit(repositoryPath, "commit -am \"feature change\"");

        // Act
        var cherryPickCommandResult = RunGitAllowFailure(repositoryPath, $"cherry-pick {sourceCommitObjectId}");
        var gitStatusSegment = await ExecuteInDirectory(repositoryPath, Program.BuildGitStatusSegmentAsync);

        // Assert
        cherryPickCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("*(feature|CHERRY-PICK)");
    }

    [Fact]
    public async Task BuildGitStatusSegment_WhenRebaseIsInProgress_ShouldShowBranchNameInsteadOfDetachedCommit()
    {
        // Arrange
        using var sandbox = new TemporaryDirectory();
        var repositoryPath = Path.Combine(sandbox.DirectoryPath, "repo");

        RunGit(sandbox.DirectoryPath, $"init --initial-branch=main {Quote(repositoryPath)}");
        ConfigureGitIdentity(repositoryPath);

        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "base\n");
        RunGit(repositoryPath, "add conflict.txt");
        RunGit(repositoryPath, "commit -m \"base\"");

        RunGit(repositoryPath, "checkout -b feature");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "feature\n");
        RunGit(repositoryPath, "commit -am \"feature change\"");

        RunGit(repositoryPath, "checkout main");
        await File.WriteAllTextAsync(Path.Combine(repositoryPath, "conflict.txt"), "main\n");
        RunGit(repositoryPath, "commit -am \"main change\"");

        RunGit(repositoryPath, "checkout feature");

        // Act
        var rebaseCommandResult = RunGitAllowFailure(repositoryPath, "rebase main");
        var gitStatusSegment = await ExecuteInDirectory(repositoryPath, Program.BuildGitStatusSegmentAsync);

        // Assert
        rebaseCommandResult.ExitCode.Should().NotBe(0);
        gitStatusSegment.Should().Contain("feature|REBASE");
        gitStatusSegment.Should().NotContain("...|REBASE");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "Prompt.Tests.Integration", Guid.NewGuid().ToString("N"));
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

    private static async Task<string> ExecuteInDirectory(string directoryPath, Func<Task<string>> operation)
    {
        var previousDirectoryPath = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(directoryPath);
            return await operation();
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectoryPath);
        }
    }

    private static void ConfigureGitIdentity(string repositoryPath)
    {
        RunGit(repositoryPath, "config user.name \"Prompt Integration Tests\"");
        RunGit(repositoryPath, "config user.email \"prompt-integration-tests@example.com\"");
    }

    private static string RunGit(string workingDirectoryPath, string arguments)
    {
        var commandResult = RunGitAllowFailure(workingDirectoryPath, arguments);
        if (commandResult.ExitCode is not  0)
        {
            throw new InvalidOperationException($"git {arguments} failed in {workingDirectoryPath}: {commandResult.StandardError}");
        }

        return commandResult.StandardOutput;
    }

    private static GitCommandResult RunGitAllowFailure(string workingDirectoryPath, string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new GitCommandResult(process.ExitCode, standardOutput, standardError);
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private readonly record struct GitCommandResult(int ExitCode, string StandardOutput, string StandardError);
}
