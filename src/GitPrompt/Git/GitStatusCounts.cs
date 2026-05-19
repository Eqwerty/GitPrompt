namespace GitPrompt.Git;

internal sealed record GitStatusCounts(
    int StagedAdded = 0,
    int StagedModified = 0,
    int StagedDeleted = 0,
    int StagedRenamed = 0,
    int UnstagedAdded = 0,
    int UnstagedModified = 0,
    int UnstagedDeleted = 0,
    int UnstagedRenamed = 0,
    int Untracked = 0,
    int Conflicts = 0)
{
    internal bool IsDirty =>
        StagedAdded > 0 || StagedModified > 0 || StagedDeleted > 0 || StagedRenamed > 0 ||
        UnstagedAdded > 0 || UnstagedModified > 0 || UnstagedDeleted > 0 || UnstagedRenamed > 0 ||
        Untracked > 0 || Conflicts > 0;

    internal bool IsDirtyStaged =>
        (StagedAdded > 0 || StagedModified > 0 || StagedDeleted > 0 || StagedRenamed > 0) &&
        UnstagedAdded is 0 && UnstagedModified is 0 && UnstagedDeleted is 0 && UnstagedRenamed is 0 &&
        Untracked is 0 && Conflicts is 0;
}
