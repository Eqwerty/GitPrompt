namespace Prompt.Git;

internal static class GitHistoryCalculator
{
    private static readonly string[] CandidateBaseReferences = ["origin/main", "origin/master", "main", "master"];

    internal static async Task<int> ComputeLocalAheadCommitCountAsync(string repositoryRootPath)
    {
        var baseReference = await ResolveBaseReferenceAsync(repositoryRootPath);

        if (string.IsNullOrEmpty(baseReference))
        {
            return await ComputeLocalAheadCommitCountWithFallbacksAsync(repositoryRootPath);
        }

        var forkPointCommit = await RunGitCommandInRepositoryAsync(repositoryRootPath, "merge-base", "--fork-point", baseReference, "HEAD") ?? string.Empty;
        if (string.IsNullOrEmpty(forkPointCommit))
        {
            forkPointCommit = await RunGitCommandInRepositoryAsync(repositoryRootPath, "merge-base", baseReference, "HEAD") ?? string.Empty;
        }

        var commitRangeSpec = !string.IsNullOrEmpty(forkPointCommit)
            ? $"{forkPointCommit}..HEAD"
            : $"{baseReference}..HEAD";

        var commitCountOutput = await RunGitCommandInRepositoryAsync(repositoryRootPath, "rev-list", "--count", commitRangeSpec);

        return int.TryParse(commitCountOutput, out var commitCount) ? commitCount : 0;
    }

    private static async Task<int> ComputeLocalAheadCommitCountWithFallbacksAsync(string repositoryRootPath)
    {
        var remoteHeadRef = await RunGitCommandInRepositoryAsync(repositoryRootPath, "symbolic-ref", "refs/remotes/origin/HEAD");

        if (!string.IsNullOrEmpty(remoteHeadRef))
        {
            var baseReference = ExtractBranchNameFromRef(remoteHeadRef);
            if (!string.IsNullOrEmpty(baseReference))
            {
                var commitCount = await TryGetAheadCountAgainstReferenceAsync(repositoryRootPath, baseReference);
                if (commitCount.HasValue)
                {
                    return commitCount.Value;
                }
            }
        }

        foreach (var candidateReference in CandidateBaseReferences)
        {
            var commitCount = await TryGetAheadCountAgainstReferenceAsync(repositoryRootPath, candidateReference);
            if (commitCount.HasValue)
            {
                return commitCount.Value;
            }
        }

        return 0;
    }

    private static async Task<int?> TryGetAheadCountAgainstReferenceAsync(string repositoryRootPath, string baseReference)
    {
        var commitCountOutput = await RunGitCommandInRepositoryAsync(repositoryRootPath, "rev-list", "--count", $"{baseReference}..HEAD");

        return int.TryParse(commitCountOutput, out var commitCount) ? commitCount : null;
    }

    private static string ExtractBranchNameFromRef(string refPath)
    {
        var normalizedRef = refPath.Contains("->")
            ? refPath.Split("->")[1].Trim()
            : refPath;

        const string prefix = "refs/remotes/";
        if (normalizedRef.StartsWith(prefix, StringComparison.Ordinal))
        {
            return normalizedRef[prefix.Length..];
        }

        return string.Empty;
    }

    internal static async Task<(int Ahead, int Behind)> ComputeAheadBehindAgainstUpstreamAsync(string repositoryRootPath, string upstreamReference)
    {
        if (string.IsNullOrEmpty(upstreamReference))
        {
            return (Ahead: 0, Behind: 0);
        }

        var leftRightCountsOutput = await RunGitCommandInRepositoryAsync(
            repositoryRootPath,
            "rev-list",
            "--left-right",
            "--count",
            $"{upstreamReference}...HEAD"
        );

        if (string.IsNullOrWhiteSpace(leftRightCountsOutput))
        {
            return (Ahead: 0, Behind: 0);
        }

        var countParts = leftRightCountsOutput.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (countParts.Length is not 2)
        {
            return (Ahead: 0, Behind: 0);
        }

        _ = int.TryParse(countParts[0], out var commitsBehind);
        _ = int.TryParse(countParts[1], out var commitsAhead);

        return (commitsAhead, commitsBehind);
    }

    internal static string EscapeCommandLineArgument(string argument) => Utilities.EscapeCommandLineArgument(argument);

    private static async Task<string> ResolveBaseReferenceAsync(string repositoryRootPath)
    {
        var baseReference = await RunGitCommandInRepositoryAsync(repositoryRootPath, "symbolic-ref", "--quiet", "--short", "refs/remotes/origin/HEAD");
        if (!string.IsNullOrEmpty(baseReference))
        {
            return baseReference;
        }

        var upstreamReference = await RunGitCommandInRepositoryAsync(repositoryRootPath, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}");
        if (!string.IsNullOrEmpty(upstreamReference))
        {
            return "@{u}";
        }

        foreach (var candidateReference in CandidateBaseReferences)
        {
            if (candidateReference.StartsWith("origin/", StringComparison.Ordinal))
            {
                var remoteReferencePath = $"refs/remotes/{candidateReference}";
                if (await ReferenceExistsAsync(repositoryRootPath, remoteReferencePath))
                {
                    return candidateReference;
                }

                continue;
            }

            var localReferencePath = $"refs/heads/{candidateReference}";
            if (await ReferenceExistsAsync(repositoryRootPath, localReferencePath))
            {
                return candidateReference;
            }
        }

        return string.Empty;
    }

    private static async Task<bool> ReferenceExistsAsync(string repositoryRootPath, string referencePath)
    {
        var referenceOutput = await RunGitCommandInRepositoryAsync(repositoryRootPath, "show-ref", "--verify", referencePath);

        return !string.IsNullOrEmpty(referenceOutput);
    }

    private static async Task<string?> RunGitCommandInRepositoryAsync(string repositoryRootPath, params string[] args)
    {
        if (string.IsNullOrEmpty(repositoryRootPath))
        {
            return null;
        }

        var joinedArguments = string.Join(' ', args.Select(Utilities.EscapeCommandLineArgument));
        var output = await Utilities.RunProcessForOutputAsync(
            fileName: "git",
            arguments: joinedArguments,
            workingDirectory: repositoryRootPath,
            requireSuccess: true
        );

        return output?.Trim();
    }
}
