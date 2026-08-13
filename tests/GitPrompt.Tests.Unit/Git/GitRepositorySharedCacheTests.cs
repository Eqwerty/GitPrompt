using FluentAssertions;
using GitPrompt.Configuration;
using GitPrompt.Git;
using Microsoft.Extensions.Time.Testing;

namespace GitPrompt.Tests.Unit.Git;

[Collection(ConfigIsolationCollection.Name)]
public sealed class GitRepositorySharedCacheTests
{
    [Fact]
    public void TryGet_WhenEntryExistsWithinTtl_ShouldReturnCachedRepositoryContext()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new ConfigDto { Cache = new ConfigDto.CacheConfig { RepositoryTtlSeconds = 60.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work", "nested");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        GitRepositorySharedCache.Set(
            [startDirectoryPath],
            new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath));

        // Act
        var found = GitRepositorySharedCache.TryGet(startDirectoryPath, out var repositoryContext);

        // Assert
        found.Should().BeTrue();
        repositoryContext.WorkingTreePath.Should().Be(Path.GetFullPath(workingTreePath));
        repositoryContext.GitDirectoryPath.Should().Be(Path.GetFullPath(gitDirectoryPath));
    }

    [Fact]
    public void TryGet_WhenEntryIsExpired_ShouldReturnFalse()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new ConfigDto { Cache = new ConfigDto.CacheConfig { RepositoryTtlSeconds = 1.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work", "nested");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        GitRepositorySharedCache.Set(
            [startDirectoryPath],
            new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath));

        fakeClock.Advance(TimeSpan.FromMilliseconds(1200));

        // Act
        var found = GitRepositorySharedCache.TryGet(startDirectoryPath, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void TryGet_WhenCacheIsDisabled_ShouldReturnFalse()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new ConfigDto { Cache = new ConfigDto.CacheConfig { RepositoryTtlSeconds = 0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        // Set is expected to be a no-op when disabled (see Set_WhenCacheIsDisabled_ShouldNotWriteCacheFile
        // for the direct assertion on that); call it here too to make sure TryGet still misses regardless.
        GitRepositorySharedCache.Set(
            [startDirectoryPath],
            new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath));

        // Act
        var found = GitRepositorySharedCache.TryGet(startDirectoryPath, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void Set_WhenCacheIsDisabled_ShouldNotWriteCacheFile()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new ConfigDto { Cache = new ConfigDto.CacheConfig { RepositoryTtlSeconds = 0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        // Act
        GitRepositorySharedCache.Set(
            [startDirectoryPath],
            new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath));

        // Assert — disabling the cache must prevent writes, not just make reads always miss
        Directory.GetFiles(cacheDirectory.DirectoryPath, "*.cache").Should().BeEmpty();
    }

    [Fact]
    public void Set_WithMultiplePaths_ShouldWriteCacheFileForEachPathAndAllHit()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new ConfigDto { Cache = new ConfigDto.CacheConfig { RepositoryTtlSeconds = 60.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);

        var pathA = Path.Combine(cacheDirectory.DirectoryPath, "src", "featureA");
        var pathB = Path.Combine(cacheDirectory.DirectoryPath, "src");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(pathA);
        Directory.CreateDirectory(pathB);
        Directory.CreateDirectory(gitDirectoryPath);

        var context = new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath);

        // Act
        GitRepositorySharedCache.Set([pathA, pathB], context);

        // Assert – both paths should independently produce a cache hit
        var foundA = GitRepositorySharedCache.TryGet(pathA, out var contextA);
        var foundB = GitRepositorySharedCache.TryGet(pathB, out var contextB);

        foundA.Should().BeTrue();
        foundB.Should().BeTrue();
        contextA.WorkingTreePath.Should().Be(Path.GetFullPath(workingTreePath));
        contextB.WorkingTreePath.Should().Be(Path.GetFullPath(workingTreePath));

        // Two distinct cache files should exist (one per unique path hash).
        var cacheFiles = Directory.GetFiles(cacheDirectory.DirectoryPath, "*.cache");
        cacheFiles.Should().HaveCount(2);
    }

    [Fact]
    public void Set_WhenStaleCacheFilesExist_ShouldDeleteOnlyExpiredCacheFiles()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new ConfigDto { Cache = new ConfigDto.CacheConfig { RepositoryTtlSeconds = 60.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow.AddYears(1));
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);
        GitRepositorySharedCache.ResetCleanupScheduleForTesting();

        var staleCachePath = Path.Combine(cacheDirectory.DirectoryPath, "stale.cache");
        var freshCachePath = Path.Combine(cacheDirectory.DirectoryPath, "fresh.cache");
        File.WriteAllText(staleCachePath, "stale");
        File.WriteAllText(freshCachePath, "fresh");

        File.SetLastWriteTimeUtc(staleCachePath, fakeClock.GetUtcNow().UtcDateTime.AddDays(-8));
        File.SetLastWriteTimeUtc(freshCachePath, fakeClock.GetUtcNow().UtcDateTime.AddDays(-1));

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        // Act
        GitRepositorySharedCache.Set(
            [startDirectoryPath],
            new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath));

        // Assert
        File.Exists(staleCachePath).Should().BeFalse();
        File.Exists(freshCachePath).Should().BeTrue();
    }

    [Fact]
    public void Set_WhenCleanupRanRecently_ShouldSkipCleanupUntilIntervalExpires()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new ConfigDto { Cache = new ConfigDto.CacheConfig { RepositoryTtlSeconds = 60.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow.AddYears(1));
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);
        GitRepositorySharedCache.ResetCleanupScheduleForTesting();

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        var context = new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath);

        // First write triggers cleanup and sets next cleanup time.
        GitRepositorySharedCache.Set([startDirectoryPath], context);

        var staleCachePath = Path.Combine(cacheDirectory.DirectoryPath, "stale.cache");
        File.WriteAllText(staleCachePath, "stale");
        File.SetLastWriteTimeUtc(staleCachePath, fakeClock.GetUtcNow().UtcDateTime.AddDays(-8));

        // Act – second write within the cleanup interval should NOT trigger another cleanup.
        GitRepositorySharedCache.Set([startDirectoryPath], context);

        // Assert – stale file should still be there because cleanup was skipped.
        File.Exists(staleCachePath).Should().BeTrue();
    }

    [Fact]
    public void Set_WhenCleanupScheduleWasPersistedByAPriorProcess_ShouldSkipCleanupUntilIntervalExpires()
    {
        // Arrange
        using var cacheDirectory = new TemporaryDirectory();
        using var configOverride = ConfigReader.OverrideForTesting(new ConfigDto { Cache = new ConfigDto.CacheConfig { RepositoryTtlSeconds = 60.0 } });
        using var cacheDirectoryOverride = GitRepositorySharedCache.OverrideCacheDirectoryForTesting(cacheDirectory.DirectoryPath);
        var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow.AddYears(1));
        using var timeOverride = GitRepositorySharedCache.OverrideTimeProviderForTesting(fakeClock);
        GitRepositorySharedCache.ResetCleanupScheduleForTesting();

        // Hand-write the schedule marker as if a *different, prior* process wrote it — this test
        // process never calls TryCleanupStaleEntries before this point, so honoring this value can
        // only come from disk, never from in-memory state (there is none left to lean on).
        var schedulePath = Path.Combine(cacheDirectory.DirectoryPath, "cleanup-schedule.token");
        var futureNextCleanupUtcTicks = fakeClock.GetUtcNow().AddMinutes(5).UtcTicks;
        File.WriteAllText(schedulePath, futureNextCleanupUtcTicks.ToString());

        var staleCachePath = Path.Combine(cacheDirectory.DirectoryPath, "stale.cache");
        File.WriteAllText(staleCachePath, "stale");
        File.SetLastWriteTimeUtc(staleCachePath, fakeClock.GetUtcNow().UtcDateTime.AddDays(-8));

        var startDirectoryPath = Path.Combine(cacheDirectory.DirectoryPath, "work");
        var workingTreePath = Path.Combine(cacheDirectory.DirectoryPath, "repo");
        var gitDirectoryPath = Path.Combine(workingTreePath, ".git");

        Directory.CreateDirectory(startDirectoryPath);
        Directory.CreateDirectory(gitDirectoryPath);

        // Act – this test process's first and only Set call; a purely in-memory schedule would
        // default to "due now" here and delete the stale file, so this proves the skip came from disk.
        GitRepositorySharedCache.Set(
            [startDirectoryPath],
            new GitRepositoryLocator.RepositoryContext(workingTreePath, gitDirectoryPath));

        // Assert
        File.Exists(staleCachePath).Should().BeTrue();
    }
}
