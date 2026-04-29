namespace GitPrompt.Constants;

internal static class AnsiColorConverter
{
    internal static string ToAnsi(string hexColor)
    {
        if (hexColor.Length != 7 || hexColor[0] != '#')
        {
            throw new ArgumentException($"Invalid hex color: '{hexColor}'. Expected format: #RRGGBB.", nameof(hexColor));
        }

        var r = Convert.ToInt32(hexColor[1..3], 16);
        var g = Convert.ToInt32(hexColor[3..5], 16);
        var b = Convert.ToInt32(hexColor[5..7], 16);

        return $"\e[38;2;{r};{g};{b}m";
    }
}
