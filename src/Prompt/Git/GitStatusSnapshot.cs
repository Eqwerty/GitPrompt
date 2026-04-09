namespace Prompt.Git;

internal sealed record GitStatusSnapshot(
    string BranchHeadName,
    string HeadObjectId,
    int CommitsAhead,
    int CommitsBehind,
    string UpstreamReference,
    bool HasUpstream,
    bool HasAheadBehindCounts,
    StatusCounts StatusCounts);
