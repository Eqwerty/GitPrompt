namespace Prompt;

internal sealed class StatusCounts
{
    internal StatusCounts(
        int stagedAdded = 0,
        int stagedModified = 0,
        int stagedDeleted = 0,
        int stagedRenamed = 0,
        int unstagedAdded = 0,
        int unstagedModified = 0,
        int unstagedDeleted = 0,
        int unstagedRenamed = 0,
        int untracked = 0,
        int conflicts = 0)
    {
        StagedAdded = stagedAdded;
        StagedModified = stagedModified;
        StagedDeleted = stagedDeleted;
        StagedRenamed = stagedRenamed;
        UnstagedAdded = unstagedAdded;
        UnstagedModified = unstagedModified;
        UnstagedDeleted = unstagedDeleted;
        UnstagedRenamed = unstagedRenamed;
        Untracked = untracked;
        Conflicts = conflicts;
    }

    public int StagedAdded { get; private set; }

    public int StagedModified { get; private set; }

    public int StagedDeleted { get; private set; }

    public int StagedRenamed { get; private set; }

    public int UnstagedAdded { get; private set; }

    public int UnstagedModified { get; private set; }

    public int UnstagedDeleted { get; private set; }

    public int UnstagedRenamed { get; private set; }

    public int Untracked { get; private set; }

    public int Conflicts { get; private set; }

    internal void TrackUntrackedFile()
    {
        Untracked++;
    }

    internal void TrackConflict()
    {
        Conflicts++;
    }

    internal void TrackStagedStatusCode(char value)
    {
        TrackStatusCode(value, isStaged: true);
    }

    internal void TrackUnstagedStatusCode(char value)
    {
        TrackStatusCode(value, isStaged: false);
    }

    private void TrackStatusCode(char value, bool isStaged)
    {
        switch (value, isStaged)
        {
            case (value: 'A', isStaged: true):
            {
                StagedAdded++;
                break;
            }
            case (value: 'A', isStaged: false):
            {
                UnstagedAdded++;
                break;
            }
            case (value: 'M', isStaged: true):
            {
                StagedModified++;
                break;
            }
            case (value: 'M', isStaged: false):
            {
                UnstagedModified++;
                break;
            }
            case (value: 'D', isStaged: true):
            {
                StagedDeleted++;
                break;
            }
            case (value: 'D', isStaged: false):
            {
                UnstagedDeleted++;
                break;
            }
            case (value: 'R' or 'C', isStaged: true):
            {
                StagedRenamed++;
                break;
            }
            case (value: 'R' or 'C', isStaged: false):
            {
                UnstagedRenamed++;
                break;
            }
            case (value: 'U', isStaged: _):
            {
                Conflicts++;
                break;
            }
        }
    }
}
