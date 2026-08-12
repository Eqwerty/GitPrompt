namespace GitPrompt.Constants;

internal static class BranchLabelTokens
{
    internal const string NoUpstreamBranchMarker = "*";
    internal const string GoneUpstreamBranchMarker = "!";
    internal const string DetachedHeadBranchMarker = ":";
    internal const string BranchLabelOpen = "(";
    internal const string BranchLabelClose = ")";
    internal const string DetachedBranchLabelOpen = "[";
    internal const string DetachedBranchLabelClose = "]";
    internal const string BranchOperationSeparator = "|";
}

