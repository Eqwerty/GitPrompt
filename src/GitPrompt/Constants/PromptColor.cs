namespace GitPrompt.Constants;

internal readonly record struct PromptColor(string Value)
{
    public static implicit operator string(PromptColor color)
    {
        return color.Value;
    }

    public override string ToString()
    {
        return Value;
    }

    public string Wrap(string text)
    {
        return $"{Value}{text}{PromptColors.ColorReset}";
    }
}
