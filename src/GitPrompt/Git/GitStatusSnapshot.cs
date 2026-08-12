namespace GitPrompt.Git;

internal sealed record GitStatusSnapshot(
    string BranchHeadName,
    string HeadObjectId,
    int CommitsAhead,
    int CommitsBehind,
    int StashEntryCount,
    bool HasUpstream,
    bool HasAheadBehindCounts,
    GitStatusCounts GitStatusCounts);
