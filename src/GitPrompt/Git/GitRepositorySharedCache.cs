using GitPrompt.Configuration;
using GitPrompt.Diagnostics;
using GitPrompt.Platform;

namespace GitPrompt.Git;

internal static class GitRepositorySharedCache
{
    private const string CacheDirectoryName = "repository-cache";
    private static readonly SharedCacheUtilities.CacheRuntimeState RuntimeState = new(TimeSpan.FromDays(7), TimeSpan.FromMinutes(5));

    internal static bool TryGet(string startDirectoryPath, out GitRepositoryLocator.RepositoryContext repositoryContext)
    {
        repositoryContext = default;

        try
        {
            if (!IsCacheEnabled())
            {
                PromptDiagnostics.RecordRepoCacheMiss(RepoCacheMissReason.Disabled);

                return false;
            }

            var normalizedStartDirectoryPath = SharedCacheUtilities.NormalizePathOrEmpty(startDirectoryPath);
            if (string.IsNullOrEmpty(normalizedStartDirectoryPath))
            {
                return false;
            }

            var cacheFilePath = GetCacheFilePath(normalizedStartDirectoryPath);
            if (!File.Exists(cacheFilePath))
            {
                PromptDiagnostics.RecordRepoCacheMiss(RepoCacheMissReason.NoEntry);

                return false;
            }

            var cacheContent = File.ReadAllText(cacheFilePath);
            if (!TryParseRecord(cacheContent, out var cacheRecord) ||
                !string.Equals(cacheRecord.StartDirectoryPath, normalizedStartDirectoryPath, Utilities.FileSystemPathComparison))
            {
                PromptDiagnostics.RecordRepoCacheMiss(RepoCacheMissReason.ParseError);

                return false;
            }

            var cacheAge = RuntimeState.GetUtcNow() - new DateTimeOffset(cacheRecord.CachedAtUtcTicks, TimeSpan.Zero);
            if (cacheAge > GetCacheTtl())
            {
                PromptDiagnostics.RecordRepoCacheMiss(RepoCacheMissReason.TtlExpired);

                return false;
            }

            repositoryContext = new GitRepositoryLocator.RepositoryContext(cacheRecord.WorkingTreePath, cacheRecord.GitDirectoryPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static void Set(IEnumerable<string> startDirectoryPaths, GitRepositoryLocator.RepositoryContext repositoryContext)
    {
        try
        {
            if (!IsCacheEnabled())
            {
                return;
            }

            var cacheDirectoryPath = GetCacheDirectoryPath();
            Directory.CreateDirectory(cacheDirectoryPath);

            var cachedAtUtcTicks = RuntimeState.GetUtcNow().Ticks;
            foreach (var startDirectoryPath in startDirectoryPaths)
            {
                var normalizedStartDirectoryPath = SharedCacheUtilities.NormalizePathOrEmpty(startDirectoryPath);
                if (string.IsNullOrEmpty(normalizedStartDirectoryPath))
                {
                    continue;
                }

                var cacheRecord = new RepositorySharedCacheRecord(
                    normalizedStartDirectoryPath,
                    SharedCacheUtilities.NormalizePathOrEmpty(repositoryContext.WorkingTreePath),
                    SharedCacheUtilities.NormalizePathOrEmpty(repositoryContext.GitDirectoryPath),
                    cachedAtUtcTicks);

                var cacheFilePath = GetCacheFilePath(normalizedStartDirectoryPath);
                SharedCacheUtilities.WriteAtomically(cacheFilePath, SerializeRecord(cacheRecord));
            }

            RuntimeState.TryCleanupStaleEntries(cacheDirectoryPath);
        }
        catch
        {
            // Keep cache as a best-effort optimization.
        }
    }

    internal static void ResetCleanupScheduleForTesting()
    {
        RuntimeState.ResetCleanupScheduleForTesting();
    }

    internal static IDisposable OverrideTimeProviderForTesting(TimeProvider timeProvider)
    {
        return RuntimeState.OverrideTimeProviderForTesting(timeProvider);
    }

    internal static IDisposable OverrideCacheDirectoryForTesting(string cacheDirectoryPath)
    {
        return RuntimeState.OverrideCacheDirectoryForTesting(cacheDirectoryPath);
    }

    private static bool IsCacheEnabled()
    {
        return GetCacheTtl() > TimeSpan.Zero;
    }

    private static TimeSpan GetCacheTtl()
    {
        var ttl = ConfigReader.Config.Cache.RepositoryTtl;

        return ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero;
    }

    private static string GetCacheDirectoryPath()
    {
        return RuntimeState.GetCacheDirectoryPath(Path.Combine(XdgPaths.GetCacheDirectory(), CacheDirectoryName));
    }

    private static string GetCacheFilePath(string normalizedStartDirectoryPath)
    {
        return Path.Combine(GetCacheDirectoryPath(), SharedCacheUtilities.HashPath(normalizedStartDirectoryPath) + ".cache");
    }

    private static string[] SerializeRecord(RepositorySharedCacheRecord cacheRecord)
    {
        return
        [
            cacheRecord.CachedAtUtcTicks.ToString(),
            SharedCacheUtilities.Encode(cacheRecord.StartDirectoryPath),
            SharedCacheUtilities.Encode(cacheRecord.WorkingTreePath),
            SharedCacheUtilities.Encode(cacheRecord.GitDirectoryPath)
        ];
    }

    private static bool TryParseRecord(string fileContent, out RepositorySharedCacheRecord cacheRecord)
    {
        cacheRecord = default;
        var lines = fileContent.Split('\n');
        var i = 0;

        if (NextLine() is not { } ticksText || !long.TryParse(ticksText, out var cachedAtUtcTicks)) { return false; }

        var startDirEncoded = NextLine();
        var workingTreeEncoded = NextLine();
        var gitDirEncoded = NextLine();
        if (startDirEncoded is null || workingTreeEncoded is null || gitDirEncoded is null)
        {
            return false;
        }

        var startDirectoryPath = SharedCacheUtilities.Decode(startDirEncoded);
        var workingTreePath = SharedCacheUtilities.Decode(workingTreeEncoded);
        var gitDirectoryPath = SharedCacheUtilities.Decode(gitDirEncoded);
        if (string.IsNullOrEmpty(startDirectoryPath) || string.IsNullOrEmpty(workingTreePath) || string.IsNullOrEmpty(gitDirectoryPath))
        {
            return false;
        }

        cacheRecord = new RepositorySharedCacheRecord(
            startDirectoryPath,
            workingTreePath,
            gitDirectoryPath,
            cachedAtUtcTicks);

        return true;

        string? NextLine() => i < lines.Length ? lines[i++].TrimEnd('\r') : null;
    }


    private readonly record struct RepositorySharedCacheRecord(
        string StartDirectoryPath,
        string WorkingTreePath,
        string GitDirectoryPath,
        long CachedAtUtcTicks);
}

