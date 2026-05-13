namespace GitPrompt.Constants;

internal static class AnsiColorConverter
{
    internal static string ToAnsi(string color)
    {
        ArgumentNullException.ThrowIfNull(color);

        if (color.Length > 0 && color[0] is '#')
        {
            return HexToAnsi(color);
        }

        return NormalizeAnsi(color);
    }

    private static string HexToAnsi(string hexColor)
    {
        if (hexColor.Length is not 7)
        {
            throw new ArgumentException($"Invalid hex color: '{hexColor}'. Expected format: #RRGGBB.", nameof(hexColor));
        }

        for (var i = 1; i < 7; i++)
        {
            if (!IsHexDigit(hexColor[i]))
            {
                throw new ArgumentException($"Invalid hex color: '{hexColor}'. Expected format: #RRGGBB.", nameof(hexColor));
            }
        }

        var r = Convert.ToInt32(hexColor[1..3], 16);
        var g = Convert.ToInt32(hexColor[3..5], 16);
        var b = Convert.ToInt32(hexColor[5..7], 16);

        return $"\e[38;2;{r};{g};{b}m";
    }

    private static string NormalizeAnsi(string input)
    {
        var code = input.AsSpan();

        if (code.Length > 0 && code[0] == '\e')
        {
            code = code[1..];
        }
        else if (code.Length > 1 && code[0] == '\\' && code[1] == 'e')
        {
            code = code[2..];
        }

        if (code.Length > 0 && code[0] == '[')
        {
            code = code[1..];
        }

        if (code.Length > 0 && code[^1] == 'm')
        {
            code = code[..^1];
        }

        if (code.Length == 0)
        {
            throw new ArgumentException($"Invalid ANSI color code: '{input}'.", nameof(input));
        }

        foreach (var c in code)
        {
            if (!char.IsAsciiDigit(c) && c != ';')
            {
                throw new ArgumentException($"Invalid ANSI color code: '{input}'. Expected numeric codes like '32', '1;33', etc.", nameof(input));
            }
        }

        return $"\e[{code}m";
    }

    private static bool IsHexDigit(char c)
    {
        return c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }
}
