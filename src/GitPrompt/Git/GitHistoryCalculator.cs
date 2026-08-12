using static GitPrompt.Git.Utilities;

namespace GitPrompt.Git;

internal static class GitHistoryCalculator
{
    private static readonly string[] CandidateBaseReferences = ["origin/main", "origin/master", "main", "master"];

    internal static int ComputeLocalAheadCommitCount(string repositoryRootPath, string currentBranchName)
    {
        var baseReference = ResolveBaseReference(repositoryRootPath, currentBranchName);

        if (string.IsNullOrEmpty(baseReference))
        {
            var totalCountOutput = RunGitCommand(repositoryRootPath, "rev-list", "--count", "HEAD");

            return int.TryParse(totalCountOutput, out var totalCount) ? totalCount : 0;
        }

        var forkPointCommit = RunGitCommand(repositoryRootPath, "merge-base", "--fork-point", baseReference, "HEAD") ?? string.Empty;
        if (string.IsNullOrEmpty(forkPointCommit))
        {
            forkPointCommit = RunGitCommand(repositoryRootPath, "merge-base", baseReference, "HEAD") ?? string.Empty;
        }

        var commitRangeSpec = !string.IsNullOrEmpty(forkPointCommit)
            ? $"{forkPointCommit}..HEAD"
            : $"{baseReference}..HEAD";

        var commitCountOutput = RunGitCommand(repositoryRootPath, "rev-list", "--count", commitRangeSpec);

        return int.TryParse(commitCountOutput, out var commitCount) ? commitCount : 0;
    }

    private static string ResolveBaseReference(string repositoryRootPath, string currentBranchName)
    {
        var baseReference = RunGitCommand(repositoryRootPath, "symbolic-ref", "--quiet", "--short", "refs/remotes/origin/HEAD");
        if (!string.IsNullOrEmpty(baseReference))
        {
            return baseReference;
        }

        var upstreamReference = RunGitCommand(repositoryRootPath, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}");
        if (!string.IsNullOrEmpty(upstreamReference))
        {
            return "@{u}";
        }

        foreach (var candidateReference in CandidateBaseReferences)
        {
            if (IsSameBranch(candidateReference, currentBranchName))
            {
                continue;
            }

            if (candidateReference.StartsWith("origin/", StringComparison.Ordinal))
            {
                var remoteReferencePath = $"refs/remotes/{candidateReference}";
                if (ReferenceExists(repositoryRootPath, remoteReferencePath))
                {
                    return candidateReference;
                }

                continue;
            }

            var localReferencePath = $"refs/heads/{candidateReference}";
            if (ReferenceExists(repositoryRootPath, localReferencePath))
            {
                return candidateReference;
            }
        }

        return string.Empty;
    }

    private static bool IsSameBranch(string candidateReference, string currentBranchName)
    {
        var shortName = candidateReference.StartsWith("origin/", StringComparison.Ordinal)
            ? candidateReference["origin/".Length..]
            : candidateReference;

        return string.Equals(shortName, currentBranchName, StringComparison.Ordinal);
    }

    private static bool ReferenceExists(string repositoryRootPath, string referencePath)
    {
        var referenceOutput = RunGitCommand(repositoryRootPath, "show-ref", "--verify", referencePath);

        return !string.IsNullOrEmpty(referenceOutput);
    }
}
