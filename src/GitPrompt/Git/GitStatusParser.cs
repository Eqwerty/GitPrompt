namespace GitPrompt.Git;

internal static class GitStatusParser
{
    private const string BranchHeadPrefix = "# branch.head ";
    private const string BranchOidPrefix = "# branch.oid ";
    private const string BranchAheadBehindPrefix = "# branch.ab ";
    private const string BranchUpstreamPrefix = "# branch.upstream ";
    private const string StashPrefix = "# stash ";

    internal static GitStatusSnapshot Parse(string statusOutput)
    {
        var branchHeadName = string.Empty;
        var headObjectId = string.Empty;
        var commitsAhead = 0;
        var commitsBehind = 0;
        var stashEntryCount = 0;
        var upstreamReference = string.Empty;
        var hasUpstream = false;
        var hasAheadBehindCounts = false;
        var stagedAdded = 0;
        var stagedModified = 0;
        var stagedDeleted = 0;
        var stagedRenamed = 0;
        var unstagedAdded = 0;
        var unstagedModified = 0;
        var unstagedDeleted = 0;
        var unstagedRenamed = 0;
        var untracked = 0;
        var conflicts = 0;

        using var reader = new StringReader(statusOutput);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0) continue;

            if (line.StartsWith(BranchHeadPrefix, StringComparison.Ordinal))
            {
                branchHeadName = line[BranchHeadPrefix.Length..];
                continue;
            }

            if (line.StartsWith(BranchOidPrefix, StringComparison.Ordinal))
            {
                headObjectId = line[BranchOidPrefix.Length..];
                continue;
            }

            if (line.StartsWith(BranchUpstreamPrefix, StringComparison.Ordinal))
            {
                upstreamReference = line[BranchUpstreamPrefix.Length..];
                hasUpstream = true;
                continue;
            }

            if (line.StartsWith(BranchAheadBehindPrefix, StringComparison.Ordinal))
            {
                ParseAheadBehind(line[BranchAheadBehindPrefix.Length..], out commitsAhead, out commitsBehind);
                hasUpstream = true;
                hasAheadBehindCounts = true;
                continue;
            }

            if (line.StartsWith(StashPrefix, StringComparison.Ordinal))
            {
                _ = int.TryParse(line[StashPrefix.Length..].Trim(), out stashEntryCount);
                continue;
            }

            if (line.Length >= 2 && line[0] == '?' && line[1] == ' ') { untracked++; continue; }
            if (line.Length >= 2 && line[0] == 'u' && line[1] == ' ') { conflicts++; continue; }

            if (line.Length >= 4 && line[0] is '1' or '2')
            {
                switch (line[2])
                {
                    case 'A': stagedAdded++; break;
                    case 'M': stagedModified++; break;
                    case 'D': stagedDeleted++; break;
                    case 'R' or 'C': stagedRenamed++; break;
                    case 'U': conflicts++; break;
                }
                switch (line[3])
                {
                    case 'A': unstagedAdded++; break;
                    case 'M': unstagedModified++; break;
                    case 'D': unstagedDeleted++; break;
                    case 'R' or 'C': unstagedRenamed++; break;
                    case 'U': conflicts++; break;
                }
            }
        }

        return new GitStatusSnapshot(
            branchHeadName,
            headObjectId,
            commitsAhead,
            commitsBehind,
            stashEntryCount,
            upstreamReference,
            hasUpstream,
            hasAheadBehindCounts,
            new GitStatusCounts(
                stagedAdded, stagedModified, stagedDeleted, stagedRenamed,
                unstagedAdded, unstagedModified, unstagedDeleted, unstagedRenamed,
                untracked, conflicts));
    }

    private static void ParseAheadBehind(string value, out int commitsAhead, out int commitsBehind)
    {
        commitsAhead = 0;
        commitsBehind = 0;

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return;

        _ = int.TryParse(parts[0].TrimStart('+'), out commitsAhead);
        _ = int.TryParse(parts[1].TrimStart('-'), out commitsBehind);
    }
}
