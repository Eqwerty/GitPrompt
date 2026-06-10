namespace GitPrompt.Constants;

internal static class BranchLabelTokens
{
    internal const string NoUpstreamBranchMarker = "*";
    internal const string GoneUpstreamBranchMarker = "!";
    internal const string DetachedHeadBranchMarker = ":";
    internal const string NormalBranchLabelOpen = "(";
    internal const string NormalBranchLabelClose = ")";
    internal const string NoUpstreamBranchLabelOpen = "(";
    internal const string NoUpstreamBranchLabelClose = ")";
    internal const string GoneUpstreamBranchLabelOpen = "(";
    internal const string GoneUpstreamBranchLabelClose = ")";
    internal const string DetachedBranchLabelOpen = "[";
    internal const string DetachedBranchLabelClose = "]";
    internal const string BranchOperationSeparator = "|";
}

