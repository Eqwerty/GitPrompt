using System.Text;

namespace GitPrompt.Git;

internal static class SharedCacheUtilities
{
    internal static void WriteAtomically(string targetFilePath, string[] lines)
    {
        var tempFilePath = targetFilePath + "." + Path.GetRandomFileName() + ".tmp";

        try
        {
            File.WriteAllLines(tempFilePath, lines);
            File.Move(tempFilePath, targetFilePath, overwrite: true);
            tempFilePath = null;
        }
        finally
        {
            if (tempFilePath is not null)
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception)
                {
                    /* best-effort temp file cleanup */
                }
            }
        }
    }

    internal static void CleanupStaleEntries(string cacheDirectoryPath, DateTime staleBeforeUtc)
    {
        foreach (var cacheFilePath in Directory.EnumerateFiles(cacheDirectoryPath, "*.cache"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(cacheFilePath) < staleBeforeUtc)
                {
                    File.Delete(cacheFilePath);
                }
            }
            catch
            {
                // Keep cleanup as best-effort and never fail prompt rendering.
            }
        }
    }

    internal static string HashPath(string value)
    {
        var hash = Fnv1A64(Encoding.UTF8.GetBytes(value));
        
        return hash.ToString("x16");
    }

    private static ulong Fnv1A64(byte[] data)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offsetBasis;
        foreach (var b in data)
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }

    internal static string NormalizePathOrEmpty(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? string.Empty : Utilities.NormalizePath(path);
    }

    internal static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    internal static string Decode(string encoded)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch
        {
            return string.Empty;
        }
    }

    internal sealed class CacheRuntimeState(TimeSpan staleCacheEntryThreshold, TimeSpan cleanupInterval)
    {
        private const string CleanupScheduleFileName = "cleanup-schedule.token";

        private TimeProvider _timeProvider = TimeProvider.System;
        private string? _cacheDirectoryOverride;

        internal DateTimeOffset GetUtcNow()
        {
            return _timeProvider.GetUtcNow();
        }

        internal string GetCacheDirectoryPath(string defaultCacheDirectoryPath)
        {
            return _cacheDirectoryOverride ?? defaultCacheDirectoryPath;
        }

        internal void TryCleanupStaleEntries(string cacheDirectoryPath)
        {
            var utcNow = GetUtcNow();
            var schedulePath = GetCleanupSchedulePath(cacheDirectoryPath);
            var nextCleanupUtcTicks = ReadNextCleanupUtcTicks(schedulePath);
            if (utcNow.UtcTicks < nextCleanupUtcTicks)
            {
                return;
            }

            CleanupStaleEntries(cacheDirectoryPath, utcNow.UtcDateTime - staleCacheEntryThreshold);
            WriteNextCleanupUtcTicks(schedulePath, utcNow.Add(cleanupInterval).UtcTicks);
        }

        internal IDisposable OverrideTimeProviderForTesting(TimeProvider timeProvider)
        {
            var previousTimeProvider = _timeProvider;
            _timeProvider = timeProvider;

            return new Utilities.RestoreAction(() => _timeProvider = previousTimeProvider);
        }

        internal IDisposable OverrideCacheDirectoryForTesting(string cacheDirectoryPath)
        {
            _cacheDirectoryOverride = cacheDirectoryPath;

            return new Utilities.RestoreAction(() => _cacheDirectoryOverride = null);
        }

        internal void ResetCleanupScheduleForTesting(string cacheDirectoryPath)
        {
            try
            {
                var schedulePath = GetCleanupSchedulePath(cacheDirectoryPath);
                if (File.Exists(schedulePath))
                {
                    File.Delete(schedulePath);
                }
            }
            catch
            {
                // Best-effort; matches this type's best-effort philosophy elsewhere.
            }
        }

        private static string GetCleanupSchedulePath(string cacheDirectoryPath)
        {
            return Path.Combine(cacheDirectoryPath, CleanupScheduleFileName);
        }

        private static long ReadNextCleanupUtcTicks(string schedulePath)
        {
            try
            {
                return File.Exists(schedulePath) && long.TryParse(File.ReadAllText(schedulePath), out var ticks)
                    ? ticks
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static void WriteNextCleanupUtcTicks(string schedulePath, long ticks)
        {
            try
            {
                WriteAtomically(schedulePath, [ticks.ToString()]);
            }
            catch
            {
                // Best-effort; a failed write here just means cleanup may run again next time.
            }
        }
    }

    internal struct FingerprintHasher()
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        private ulong _hash = OffsetBasis;

        public void AppendString(string value)
        {
            AppendBytes(Encoding.UTF8.GetBytes(value));
        }

        public void AppendByte(byte value)
        {
            _hash ^= value;
            _hash *= Prime;
        }

        public void AppendInt64(long value)
        {
            AppendBytes(BitConverter.GetBytes(value));
        }

        private void AppendBytes(byte[] bytes)
        {
            foreach (var b in bytes)
            {
                _hash ^= b;
                _hash *= Prime;
            }
        }

        public string GetHexString() => _hash.ToString("x16");
    }
}
