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
        if (hexColor.Length is not 7 || hexColor.Skip(1).Any(@char => !IsHexDigit(@char)))
        {
            throw new ArgumentException($"Invalid hex color: '{hexColor}'. Expected format: #RRGGBB.", nameof(hexColor));
        }

        var r = Convert.ToInt32(hexColor[1..3], fromBase: 16);
        var g = Convert.ToInt32(hexColor[3..5], fromBase: 16);
        var b = Convert.ToInt32(hexColor[5..7], fromBase: 16);

        return $"\e[38;2;{r};{g};{b}m";
    }

    private static string NormalizeAnsi(string code)
    {
        if (code.Length is 0)
        {
            throw new ArgumentException($"Invalid ANSI color code: '{code}'.", nameof(code));
        }

        if (code[0] is '\e')
        {
            code = code[1..];
        }
        else if (code.Length > 1 && code[0] is '\\' && code[1] is 'e')
        {
            code = code[2..];
        }

        if (code[0] is '[')
        {
            code = code[1..];
        }

        if (code[^1] is 'm')
        {
            code = code[..^1];
        }

        if (code.Any(@char => !char.IsAsciiDigit(@char) && @char is not ';'))
        {
            throw new ArgumentException($"Invalid ANSI color code: '{code}'. Expected numeric codes like '32', '1;33', etc.", nameof(code));
        }

        return $"\e[{code}m";
    }

    private static bool IsHexDigit(char @char)
    {
        return @char is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }
}
