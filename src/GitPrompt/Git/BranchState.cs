namespace GitPrompt.Git;

internal enum BranchState
{
    Normal,
    NoUpstream,
    GoneUpstream,
    Detached,
}
