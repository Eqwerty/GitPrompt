using BenchmarkDotNet.Attributes;
using Prompt.Git;

namespace Prompt.Benchmarks;

[MemoryDiagnoser]
public class GitStatusDisplayBenchmarks
{
    private string _gitDirectoryPath = string.Empty;

    private StatusCounts _cleanCounts = new();

    private StatusCounts _busyCounts = new(
        StagedAdded: 1,
        StagedModified: 2,
        StagedRenamed: 1,
        UnstagedModified: 2,
        UnstagedDeleted: 1,
        Untracked: 3,
        Conflicts: 1);

    [GlobalSetup]
    public void Setup()
    {
        _gitDirectoryPath = Path.Combine(Path.GetTempPath(), "Prompt.Benchmarks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_gitDirectoryPath);

        _cleanCounts = new StatusCounts();

        _busyCounts = new StatusCounts(
            StagedAdded: 1,
            StagedModified: 2,
            StagedRenamed: 1,
            UnstagedModified: 2,
            UnstagedDeleted: 1,
            Untracked: 3,
            Conflicts: 1);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_gitDirectoryPath))
        {
            Directory.Delete(_gitDirectoryPath, recursive: true);
        }
    }

    [Benchmark]
    public string BuildDisplay_CleanRepository()
    {
        return GitStatusSegmentBuilder.BuildDisplay("(main)", commitsAhead: 0, commitsBehind: 0, stashEntryCount: 0, _cleanCounts, _gitDirectoryPath);
    }

    [Benchmark]
    public string BuildDisplay_BusyRepository()
    {
        return GitStatusSegmentBuilder.BuildDisplay("(feature)", commitsAhead: 4, commitsBehind: 2, stashEntryCount: 0, _busyCounts, _gitDirectoryPath);
    }
}
