namespace GitPrompt.Commands;

internal sealed record CommandDescriptor(
    string[] Verbs,
    string Usage,
    string Description,
    Action<string[]> Execute,
    bool IsHidden = false);
