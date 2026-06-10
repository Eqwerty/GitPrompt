namespace GitPrompt.Configuration;

internal sealed record Config
{
    internal required bool Compact { get; init; }

    internal required bool ShowStash { get; init; }

    internal required double CommandTimeoutMs { get; init; }

    internal required CacheConfig Cache { get; init; }

    internal required CommandDurationConfig CommandDuration { get; init; }

    internal required ContextConfig Context { get; init; }

    internal required LayoutConfig Layout { get; init; }

    internal required IconsConfig Icons { get; init; }

    internal required ColorsConfig Colors { get; init; }

    internal TimeSpan? CommandTimeout
    {
        get
        {
            return CommandTimeoutMs > 0 && double.IsFinite(CommandTimeoutMs) ? TimeSpan.FromMilliseconds(CommandTimeoutMs) : null;
        }
    }

    internal sealed record CacheConfig
    {
        internal required TimeSpan GitStatusTtl { get; init; }

        internal required TimeSpan RepositoryTtl { get; init; }
    }

    internal sealed record CommandDurationConfig
    {
        internal required bool Show { get; init; }

        internal double? MinMs { get; init; }
    }

    internal sealed record ContextConfig
    {
        internal required bool ShowUser { get; init; }

        internal required bool ShowDomain { get; init; }

        internal required bool ShowHost { get; init; }

        internal required bool ShowPath { get; init; }

        internal required int MaxPathDepth { get; init; }
    }

    internal sealed record LayoutConfig
    {
        internal required bool Multiline { get; init; }

        internal required bool NewlineBefore { get; init; }

        internal required bool StartOfLine { get; init; }

        internal string? Symbol { get; init; }

        internal string? Prefix { get; init; }
    }

    internal sealed record IconsConfig
    {
        internal required string Ahead { get; init; }

        internal required string Behind { get; init; }

        internal required string Added { get; init; }

        internal required string Modified { get; init; }

        internal required string Renamed { get; init; }

        internal required string Deleted { get; init; }

        internal required string Untracked { get; init; }

        internal required string Conflicts { get; init; }

        internal required string Stash { get; init; }

        internal required string Dirty { get; init; }

        internal required string DirtyStaged { get; init; }

        internal required string Clean { get; init; }

        internal required string NoUpstreamMarker { get; init; }

        internal required string GoneUpstreamMarker { get; init; }

        internal required string DetachedHeadMarker { get; init; }

        internal required string BranchLabelOpenNormal { get; init; }

        internal required string BranchLabelCloseNormal { get; init; }

        internal required string BranchLabelOpenNoUpstream { get; init; }

        internal required string BranchLabelCloseNoUpstream { get; init; }

        internal required string BranchLabelOpenGoneUpstream { get; init; }

        internal required string BranchLabelCloseGoneUpstream { get; init; }

        internal required string BranchLabelOpenDetached { get; init; }

        internal required string BranchLabelCloseDetached { get; init; }

        internal required string BranchOperationSeparator { get; init; }
    }

    internal sealed record ColorsConfig
    {
        internal required string User { get; init; }

        internal required string Host { get; init; }

        internal required string Path { get; init; }

        internal required string CommandDuration { get; init; }

        internal required string Branch { get; init; }

        internal required string BranchNoUpstream { get; init; }

        internal required string BranchGoneUpstream { get; init; }

        internal required string BranchDetached { get; init; }

        internal required string Ahead { get; init; }

        internal required string Behind { get; init; }

        internal required string Staged { get; init; }

        internal required string Unstaged { get; init; }

        internal required string Untracked { get; init; }

        internal required string Stash { get; init; }

        internal required string Conflict { get; init; }

        internal required string Dirty { get; init; }

        internal required string DirtyStaged { get; init; }

        internal required string Clean { get; init; }

        internal required string MissingPath { get; init; }

        internal required string Timeout { get; init; }

        internal required string PromptSymbol { get; init; }

        internal required string Prefix { get; init; }
    }
}
