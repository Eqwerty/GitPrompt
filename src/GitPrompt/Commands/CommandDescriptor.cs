namespace GitPrompt.Commands;

internal sealed record CommandDescriptor(
    string Verb,
    string Usage,
    string Description,
    Action<string[]> Execute,
    bool IsHidden = false);
