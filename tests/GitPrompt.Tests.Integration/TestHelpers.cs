using System.Diagnostics;
using GitPrompt.Configuration;
using GitPrompt.Git;
using static GitPrompt.Constants.BranchLabelTokens;

namespace GitPrompt.Tests.Integration;

internal static class TestHelpers
{
    internal sealed class TemporaryDirectory : IDisposable
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
                try
                {
                    foreach (var filePath in Directory.EnumerateFiles(DirectoryPath, "*", SearchOption.AllDirectories))
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                    }

                    Directory.Delete(DirectoryPath, recursive: true);
                }
                catch (IOException)
                {
                    // Silently ignore cleanup errors - the OS will eventually clean up temp directories
                }
            }
        }
    }

    internal static async Task ConfigureGitIdentityAsync(string repositoryPath)
    {
        await RunGitAsync(repositoryPath, "config user.name \"Prompt Integration Tests\"");
        await RunGitAsync(repositoryPath, "config user.email \"prompt-integration-tests@example.com\"");
    }

    internal static async Task<string> RunGitAsync(string workingDirectoryPath, string arguments)
    {
        var commandResult = await RunGitAllowFailureAsync(workingDirectoryPath, arguments);
        if (commandResult.ExitCode is not 0)
        {
            throw new InvalidOperationException($"git {arguments} failed in {workingDirectoryPath}: {commandResult.StandardError}");
        }

        return commandResult.StandardOutput;
    }

    internal static async Task<GitCommandResult> RunGitAllowFailureAsync(string workingDirectoryPath, string arguments)
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
        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new GitCommandResult(process.ExitCode, standardOutput, standardError);
    }

    internal static string Quote(string value)
    {
        return "\"" + value.Replace("\\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    internal static string TrackedBranchLabel(string branchName)
    {
        return $"{BranchLabelOpen}{branchName}{BranchLabelClose}";
    }

    internal static string NoUpstreamBranchLabel(string branchName)
    {
        return $"{NoUpstreamBranchMarker}{TrackedBranchLabel(branchName)}";
    }

    internal static string BranchLabelWithOperation(string branchLabel, string operation)
    {
        return branchLabel.Replace(BranchLabelClose, $"|{operation}{BranchLabelClose}", StringComparison.Ordinal);
    }

    internal static string Indicator(char icon, int count)
    {
        return $"{icon}{count}";
    }

    internal readonly record struct GitCommandResult(int ExitCode, string StandardOutput, string StandardError);

    /// <summary>
    /// Injects a fake <c>git</c> executable that sleeps for 30 seconds by prepending a temp
    /// directory to PATH. This guarantees that any timeout shorter than 30 s fires reliably,
    /// regardless of how fast the real git binary starts on the host machine.
    /// On Windows the fake script is omitted (PATH is still patched but falls back to real git)
    /// because git startup there is already slow enough for the tests that use this.
    /// </summary>
    internal sealed class FakeSlowGitOverride : IDisposable
    {
        private readonly string? _originalPath;
        private readonly string _tempDirectory;

        public FakeSlowGitOverride()
        {
            _originalPath = Environment.GetEnvironmentVariable("PATH");
            _tempDirectory = Path.Combine(Path.GetTempPath(), "Prompt.Tests.Integration.FakeGit", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            if (!OperatingSystem.IsWindows())
            {
                var fakeGitPath = Path.Combine(_tempDirectory, "git");
                File.WriteAllText(fakeGitPath, "#!/bin/sh\nsleep 30\n");
                File.SetUnixFileMode(fakeGitPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            Environment.SetEnvironmentVariable("PATH", _tempDirectory + Path.PathSeparator + (_originalPath ?? string.Empty));
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("PATH", _originalPath);

            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
                // Silently ignore cleanup errors - the OS will eventually clean up temp directories
            }
        }
    }

    internal sealed class GitStatusCacheOverride : IDisposable
    {
        private readonly IDisposable _configOverride;
        private readonly IDisposable _cacheDirectoryOverride;

        public GitStatusCacheOverride(string cacheDirectoryPath, TimeSpan ttl = default)
        {
            var effectiveTtl = ttl == default ? TimeSpan.FromMinutes(1) : ttl;
            _configOverride = ConfigReader.OverrideForTesting(new Config
            {
                Cache = new Config.CacheConfig { GitStatusTtlSeconds = effectiveTtl.TotalSeconds }
            });

            _cacheDirectoryOverride = GitStatusSharedCache.OverrideCacheDirectoryForTesting(cacheDirectoryPath);
        }

        public void Dispose()
        {
            _configOverride.Dispose();
            _cacheDirectoryOverride.Dispose();
        }
    }
}
