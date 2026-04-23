using System.Diagnostics;
using GitPrompt.Diagnostics;

namespace GitPrompt.Git;

internal static class Utilities
{
    internal static readonly StringComparer FileSystemPathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    internal static readonly StringComparison FileSystemPathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    internal static string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }

    internal static IEnumerable<string> EnumerateLines(string text)
    {
        using var reader = new StringReader(text);
        while (reader.ReadLine() is string line)
        {
            yield return line;
        }
    }

    internal static string EscapeCommandLineArgument(string argument)
    {
        if (argument.Length is 0)
        {
            return "\"\"";
        }

        if (!argument.Any(static c => char.IsWhiteSpace(c) || c is '"'))
        {
            return argument;
        }

        return "\"" + argument.Replace("\\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    internal static string ShortenCommitHash(string commitHash)
    {
        if (string.IsNullOrEmpty(commitHash))
        {
            return string.Empty;
        }

        const int shortCommitHashLength = 7;

        return commitHash.Length >= shortCommitHashLength ? commitHash[..shortCommitHashLength] : commitHash;
    }

    // Synchronous entry point for callers that have no async context.
    //
    // .GetAwaiter().GetResult() is safe here for two independent reasons:
    //
    // 1. No SynchronizationContext deadlock: this is a console application.
    //    SynchronizationContext.Current is always null, so async continuations are
    //    scheduled on the thread pool and can never try to resume on the calling
    //    thread. The classic deadlock (continuation captures a single-threaded
    //    context and tries to re-enter the blocked caller) cannot occur.
    //
    // 2. No thread-pool starvation: this method is only ever called from the main
    //    thread (via Program.cs or DebugCommand.Run). The main thread is the OS
    //    process entry thread — it is NOT a thread-pool thread. Blocking it does
    //    not consume a pool slot, so the async continuations (ReadToEndAsync,
    //    WaitForExitAsync) can always find a free pool thread.
    internal static string? RunGitCommand(string repositoryRootPath, params string[] args)
    {
        return RunGitCommandAsync(repositoryRootPath, args).GetAwaiter().GetResult();
    }

    private static async Task<string?> RunGitCommandAsync(string repositoryRootPath, params string[] args)
    {
        if (string.IsNullOrEmpty(repositoryRootPath))
        {
            return null;
        }

        var joinedArguments = string.Join(' ', args.Select(EscapeCommandLineArgument));
        var output = await RunProcessForOutputAsync(
            fileName: "git",
            arguments: joinedArguments,
            workingDirectory: repositoryRootPath,
            requireSuccess: true
        );

        return output?.Trim();
    }

    private static async Task<string?> RunProcessForOutputAsync(string fileName, string arguments, string? workingDirectory, bool requireSuccess)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? string.Empty
            };

            var sw = PromptDiagnostics.IsEnabled ? Stopwatch.StartNew() : null;
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync());

            if (sw is not null)
            {
                sw.Stop();
                PromptDiagnostics.RecordGitSubprocessElapsed(sw.Elapsed);
            }

            if (requireSuccess && process.ExitCode is not 0)
            {
                return null;
            }

            return process.ExitCode is 0 ? stdoutTask.Result : string.Empty;
        }
        catch
        {
            return null;
        }
    }
}
