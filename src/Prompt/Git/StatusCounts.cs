namespace Prompt.Git;

internal sealed record StatusCounts(
    int StagedAdded,
    int StagedModified,
    int StagedDeleted,
    int StagedRenamed,
    int UnstagedAdded,
    int UnstagedModified,
    int UnstagedDeleted,
    int UnstagedRenamed,
    int Untracked,
    int Conflicts);
