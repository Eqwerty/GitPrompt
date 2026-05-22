using System.Text;
using GitPrompt.Configuration;
using GitPrompt.Constants;
using static GitPrompt.Constants.PromptColors;

namespace GitPrompt.Git;

internal static class GitStatusDisplayFormatter
{
    private readonly record struct CountStyle(int Value, PromptColor Color, string Icon);

    internal static string BuildDisplay(
        BranchLabelInfo branchLabel,
        int commitsAhead,
        int commitsBehind,
        int stashEntryCount,
        GitStatusCounts gitStatusCounts,
        string operationName)
    {
        var icons = ConfigReader.Config.Icons;

        var aheadIcon = icons.Ahead;
        var behindIcon = icons.Behind;
        var addedIcon = icons.Added;
        var modifiedIcon = icons.Modified;
        var renamedIcon = icons.Renamed;
        var deletedIcon = icons.Deleted;
        var untrackedIcon = icons.Untracked;
        var conflictsIcon = icons.Conflicts;
        var stashIcon = icons.Stash;

        var statusBuilder = new StringBuilder();

        var labelWithOp = AppendOperationToBranchLabel(branchLabel, operationName);

        var branchColor = branchLabel.State switch
        {
            BranchState.NoUpstream => ColorBranchNoUpstream,
            BranchState.Detached => ColorBranchDetached,
            _ => ColorBranch
        };

        statusBuilder.Append(branchColor.Wrap(labelWithOp));

        if (commitsAhead > 0)
        {
            statusBuilder.Append(' ').Append(ColorAhead.Wrap($"{aheadIcon}{commitsAhead}"));
        }

        if (commitsBehind > 0)
        {
            statusBuilder.Append(' ').Append(ColorBehind.Wrap($"{behindIcon}{commitsBehind}"));
        }

        AppendCountIndicators(
            statusBuilder,
            new CountStyle(gitStatusCounts.StagedAdded, ColorStaged, addedIcon),
            new CountStyle(gitStatusCounts.StagedModified, ColorStaged, modifiedIcon),
            new CountStyle(gitStatusCounts.StagedRenamed, ColorStaged, renamedIcon),
            new CountStyle(gitStatusCounts.StagedDeleted, ColorStaged, deletedIcon),
            new CountStyle(gitStatusCounts.UnstagedAdded, ColorUnstaged, addedIcon),
            new CountStyle(gitStatusCounts.UnstagedModified, ColorUnstaged, modifiedIcon),
            new CountStyle(gitStatusCounts.UnstagedRenamed, ColorUnstaged, renamedIcon),
            new CountStyle(gitStatusCounts.UnstagedDeleted, ColorUnstaged, deletedIcon)
        );

        if (gitStatusCounts.Untracked > 0)
        {
            statusBuilder.Append(' ').Append(ColorUntracked.Wrap($"{untrackedIcon}{gitStatusCounts.Untracked}"));
        }

        if (gitStatusCounts.Conflicts > 0)
        {
            statusBuilder.Append(' ').Append(ColorConflict.Wrap($"{conflictsIcon}{gitStatusCounts.Conflicts}"));
        }

        if (stashEntryCount > 0 && ConfigReader.Config.ShowStash)
        {
            statusBuilder.Append(' ').Append(ColorStash.Wrap($"{stashIcon}{stashEntryCount}"));
        }

        return statusBuilder.ToString();
    }

    internal static string BuildDisplayCompact(
        BranchLabelInfo branchLabel,
        int commitsAhead,
        int commitsBehind,
        int stashEntryCount,
        GitStatusCounts gitStatusCounts,
        string operationName)
    {
        var icons = ConfigReader.Config.Icons;
        var aheadIcon = icons.Ahead;
        var behindIcon = icons.Behind;
        var dirtyIcon = icons.Dirty;
        var dirtyStagedIcon = icons.DirtyStaged;
        var cleanIcon = icons.Clean;
        var stashIcon = icons.Stash;

        var statusBuilder = new StringBuilder();

        var labelWithOp = AppendOperationToBranchLabel(branchLabel, operationName);

        var branchColor = branchLabel.State switch
        {
            BranchState.NoUpstream => ColorBranchNoUpstream,
            BranchState.Detached => ColorBranchDetached,
            _ => ColorBranch
        };

        statusBuilder.Append(branchColor.Wrap(labelWithOp));

        if (commitsAhead > 0)
        {
            statusBuilder.Append(' ').Append(ColorAhead.Wrap($"{aheadIcon}{commitsAhead}"));
        }

        if (commitsBehind > 0)
        {
            statusBuilder.Append(' ').Append(ColorBehind.Wrap($"{behindIcon}{commitsBehind}"));
        }

        if (gitStatusCounts.IsDirtyStaged)
        {
            statusBuilder.Append(' ').Append(ColorDirtyStaged.Wrap(dirtyStagedIcon));
        }
        else if (gitStatusCounts.IsDirty)
        {
            statusBuilder.Append(' ').Append(ColorDirty.Wrap(dirtyIcon));
        }
        else
        {
            statusBuilder.Append(' ').Append(ColorClean.Wrap(cleanIcon));
        }

        if (stashEntryCount > 0 && ConfigReader.Config.ShowStash)
        {
            statusBuilder.Append(' ').Append(ColorStash.Wrap($"{stashIcon}{stashEntryCount}"));
        }

        return statusBuilder.ToString();
    }

    internal static BranchLabelInfo BuildBranchLabel(string branchName, BranchState state)
    {
        var icons = ConfigReader.Config.Icons;

        var (open, close) = state switch
        {
            BranchState.NoUpstream => (
                icons.BranchLabelOpenNoUpstream,
                icons.BranchLabelCloseNoUpstream),
            BranchState.Detached => (
                icons.BranchLabelOpenDetached,
                icons.BranchLabelCloseDetached),
            _ => (
                icons.BranchLabelOpenNormal,
                icons.BranchLabelCloseNormal)
        };

        var prefix = state switch
        {
            BranchState.NoUpstream => icons.NoUpstreamMarker,
            BranchState.Detached => icons.DetachedHeadMarker,
            _ => string.Empty
        };

        return new BranchLabelInfo($"{prefix}{open}{branchName}{close}", state);
    }

    private static string AppendOperationToBranchLabel(BranchLabelInfo branchLabel, string operationName)
    {
        if (string.IsNullOrEmpty(operationName))
        {
            return branchLabel.Label;
        }

        var icons = ConfigReader.Config.Icons;
        var close = branchLabel.State switch
        {
            BranchState.NoUpstream => icons.BranchLabelCloseNoUpstream,
            BranchState.Detached => icons.BranchLabelCloseDetached,
            _ => icons.BranchLabelCloseNormal
        };

        var separator = icons.BranchOperationSeparator;
        if (branchLabel.Label.EndsWith(close, StringComparison.Ordinal))
        {
            return branchLabel.Label[..^close.Length] + separator + operationName + close;
        }

        return branchLabel.Label + separator + operationName;
    }

    private static void AppendCountIndicators(StringBuilder sb, params CountStyle[] items)
    {
        foreach (var item in items)
        {
            if (item.Value > 0)
            {
                sb.Append(' ').Append(item.Color.Wrap($"{item.Icon}{item.Value}"));
            }
        }
    }
}
