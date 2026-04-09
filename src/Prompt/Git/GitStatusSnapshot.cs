namespace Prompt.Git;

internal sealed class GitStatusSnapshot
{
    public string BranchHeadName { get; init; } = string.Empty;

    public string HeadObjectId { get; init; } = string.Empty;

    public int CommitsAhead { get; init; }

    public int CommitsBehind { get; init; }

    public string UpstreamReference { get; init; } = string.Empty;

    public bool HasUpstream { get; init; }

    public bool HasAheadBehindCounts { get; init; }

    public StatusCounts StatusCounts { get; init; } = new();
}
