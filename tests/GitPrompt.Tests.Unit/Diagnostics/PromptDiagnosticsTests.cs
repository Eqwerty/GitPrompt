using FluentAssertions;
using GitPrompt.Diagnostics;
using GitPrompt.Prompting;

namespace GitPrompt.Tests.Unit.Diagnostics;

[Collection(DiagnosticsIsolationCollection.Name)]
public sealed class PromptDiagnosticsTests
{
    [Fact]
    public void GetReport_WhenStatusCacheHit_ShouldShowHitWithAge()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Hit();
        PromptDiagnostics.RecordStatusCacheHit(age: TimeSpan.FromSeconds(2), ttl: TimeSpan.FromSeconds(5));
        var result = new PromptResult("user host ~/repo", "(main)", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(3), TimeSpan.FromMilliseconds(4));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("Status cache    hit");
        report.Should().Contain("2s old");
        report.Should().Contain("TTL 5s");
        report.Should().Contain("Git segment served from cache.");
    }

    [Fact]
    public void GetReport_WhenStatusCacheMissFingerprintChanged_ShouldShowGitStateChangedAndTip()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Hit();
        PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.FingerprintChanged);
        PromptDiagnostics.RecordGitSubprocessElapsed(TimeSpan.FromMilliseconds(50));
        var result = new PromptResult("user host ~/repo", "(main) ~2", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(51), TimeSpan.FromMilliseconds(52));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("miss · git state changed");
        report.Should().Contain("ran git");
        report.Should().Contain("Cache miss caused by a real git change");
    }

    [Fact]
    public void GetReport_WhenStatusCacheMissTtlExpired_ShouldShowTtlExpiredAndTip()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Hit();
        PromptDiagnostics.RecordStatusCacheMiss(
            StatusCacheMissReason.TtlExpired,
            age: TimeSpan.FromSeconds(6),
            ttl: TimeSpan.FromSeconds(5));
        var result = new PromptResult("user host ~/repo", "(main)", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(51));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("TTL expired");
        report.Should().Contain("6s old");
        report.Should().Contain("TTL 5s");
        report.Should().Contain("Tip");
        report.Should().Contain("cache.gitStatusTtl");
    }

    [Fact]
    public void GetReport_WhenStatusCacheMissNoEntry_ShouldShowFirstRunTip()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheWalk(dirsWalked: 2, repoFound: true);
        PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.NoEntry);
        var result = new PromptResult("user host ~/repo", "(main)", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(55), TimeSpan.FromMilliseconds(56));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("miss · no entry");
        report.Should().Contain("First render or cache was evicted");
    }

    [Fact]
    public void GetReport_WhenNotInRepo_ShouldShowSkippedStatusCacheAndNoRepoMessage()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Miss(RepoCacheMissReason.NoEntry);
        PromptDiagnostics.RecordRepoCacheWalk(dirsWalked: 4, repoFound: false);
        var result = new PromptResult("user host ~/documents", string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/documents", result);

        // Assert
        report.Should().Contain("Status cache    skipped");
        report.Should().Contain("walked 4 dirs, no repo found");
        report.Should().Contain("Not in a git repository.");
    }

    [Fact]
    public void GetReport_WhenRepoCacheL1Hit_ShouldShowInProcessHit()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL1Hit();
        PromptDiagnostics.RecordStatusCacheHit(age: TimeSpan.FromSeconds(1), ttl: TimeSpan.FromSeconds(5));
        var result = new PromptResult("user host ~/repo", "(main)", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(3));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("Repository      hit (in-process)");
    }

    [Fact]
    public void GetReport_WhenRepoCacheWalkFoundRepo_ShouldShowDirCount()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Miss(RepoCacheMissReason.NoEntry);
        PromptDiagnostics.RecordRepoCacheWalk(dirsWalked: 3, repoFound: true);
        PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.NoEntry);
        var result = new PromptResult("user host ~/repo", "(main)", "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(55), TimeSpan.FromMilliseconds(56));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("no entry → walked 3 dirs");
        report.Should().NotContain("no repo found");
    }

    [Fact]
    public void RecordStatusCacheHit_WhenNotEnabled_ShouldNotBeReflectedInReport()
    {
        // Arrange — diagnostics not enabled
        PromptDiagnostics.Reset();

        // Act & Assert — calling record methods when disabled must not throw and must have no effect
        var act = () =>
        {
            PromptDiagnostics.RecordStatusCacheHit(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
            PromptDiagnostics.RecordRepoCacheL1Hit();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void GetReport_WhenStatusCacheMissDisabled_ShouldShowDisabledMessage()
    {
        // Arrange
        using var scope = PromptDiagnostics.EnableForTesting();
        PromptDiagnostics.RecordRepoCacheL2Hit();
        PromptDiagnostics.RecordStatusCacheMiss(StatusCacheMissReason.Disabled);
        var result = new PromptResult("user host ~/repo", string.Empty, "$",
            TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));

        // Act
        var report = PromptDiagnostics.GetReport("/home/user/repo", result);

        // Assert
        report.Should().Contain("miss · cache disabled");
        report.Should().Contain("Status cache is disabled");
    }
}
