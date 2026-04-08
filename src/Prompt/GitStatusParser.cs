namespace Prompt;

internal static class GitStatusParser
{
    internal static GitStatusSnapshot Parse(string statusOutput)
    {
        var branchHeadName = string.Empty;
        var headObjectId = string.Empty;
        var commitsAhead = 0;
        var commitsBehind = 0;
        var upstreamReference = string.Empty;
        var hasUpstream = false;
        var hasAheadBehindCounts = false;
        var statusCounts = new StatusCounts();

        foreach (var line in EnumerateLines(statusOutput))
        {
            const string statusBranchHeadPrefix = "# branch.head ";
            if (line.StartsWith(statusBranchHeadPrefix, StringComparison.Ordinal))
            {
                branchHeadName = line[statusBranchHeadPrefix.Length..];
                continue;
            }

            const string statusBranchOidPrefix = "# branch.oid ";
            if (line.StartsWith(statusBranchOidPrefix, StringComparison.Ordinal))
            {
                headObjectId = line[statusBranchOidPrefix.Length..];
                continue;
            }

            const string statusBranchAheadBehindPrefix = "# branch.ab ";
            if (line.StartsWith(statusBranchAheadBehindPrefix, StringComparison.Ordinal))
            {
                var parts = line[statusBranchAheadBehindPrefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length >= 2)
                {
                    _ = int.TryParse(parts[0].TrimStart('+'), out commitsAhead);
                    _ = int.TryParse(parts[1].TrimStart('-'), out commitsBehind);
                }

                hasUpstream = true;
                hasAheadBehindCounts = true;
                continue;
            }

            const string statusBranchUpstreamPrefix = "# branch.upstream ";
            if (line.StartsWith(statusBranchUpstreamPrefix, StringComparison.Ordinal))
            {
                upstreamReference = line[statusBranchUpstreamPrefix.Length..];
                hasUpstream = true;
                continue;
            }

            var isUntrackedRecord = line.StartsWith("? ", StringComparison.Ordinal);
            var isUnmergedRecord = line.StartsWith("u ", StringComparison.Ordinal);
            if (isUntrackedRecord || isUnmergedRecord)
            {
                if (isUntrackedRecord)
                {
                    statusCounts.TrackUntrackedFile();
                }
                else
                {
                    statusCounts.TrackConflict();
                }

                continue;
            }

            var isOrdinaryTrackedEntryRecord = line.Length >= 4 && line[0] is '1';
            var isRenamedOrCopiedTrackedEntryRecord = line.Length >= 4 && line[0] is '2';
            if (isOrdinaryTrackedEntryRecord || isRenamedOrCopiedTrackedEntryRecord)
            {
                var stagedStatusCode = line[2];
                var unstagedStatusCode = line[3];
                statusCounts.TrackStagedStatusCode(stagedStatusCode);
                statusCounts.TrackUnstagedStatusCode(unstagedStatusCode);
            }
        }

        return new GitStatusSnapshot
        {
            BranchHeadName = branchHeadName,
            HeadObjectId = headObjectId,
            CommitsAhead = commitsAhead,
            CommitsBehind = commitsBehind,
            UpstreamReference = upstreamReference,
            HasUpstream = hasUpstream,
            HasAheadBehindCounts = hasAheadBehindCounts,
            StatusCounts = statusCounts
        };
    }

    private static IEnumerable<string> EnumerateLines(string text)
    {
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}
