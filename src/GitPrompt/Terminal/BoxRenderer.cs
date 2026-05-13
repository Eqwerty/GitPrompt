using System.Text;
using GitPrompt.Constants;

namespace GitPrompt.Terminal;

internal static class BoxRenderer
{
    private const char TopLeft = '╭';
    private const char TopRight = '╮';
    private const char BottomLeft = '╰';
    private const char BottomRight = '╯';
    private const char Horizontal = '─';
    private const char Vertical = '│';
    private const char SeparatorLeft = '├';
    private const char SeparatorRight = '┤';

    internal static string Render(string title, IReadOnlyList<string?> lines)
    {
        var innerWidth = ComputeInnerWidth(title, lines);
        var ansiColor = AnsiColors.White;
        var reset = AnsiColors.Reset;

        var sb = new StringBuilder();

        AppendTopBorder(sb, title, innerWidth, ansiColor, reset);

        foreach (var line in lines)
        {
            if (line is null)
            {
                AppendSeparator(sb, innerWidth, ansiColor, reset);
            }
            else
            {
                AppendContentLine(sb, line, innerWidth, ansiColor, reset);
            }
        }

        AppendBottomBorder(sb, innerWidth, ansiColor, reset);

        return sb.ToString();
    }

    private static int ComputeInnerWidth(string title, IReadOnlyList<string?> lines)
    {
        var minForTitle = title.Length + 4;

        var maxLineLength = 0;
        foreach (var line in lines)
        {
            if (line is not null && line.Length > maxLineLength)
            {
                maxLineLength = line.Length;
            }
        }

        var minForContent = maxLineLength + 2;

        return Math.Max(minForTitle, minForContent);
    }

    private static void AppendTopBorder(StringBuilder sb, string title, int innerWidth, string borderColor, string reset)
    {
        var dashesBeforeTitle = 2;
        var totalDashesAfterTitle = innerWidth - title.Length - dashesBeforeTitle - 2;

        sb.Append(borderColor);
        sb.Append(TopLeft);
        sb.Append(Horizontal, dashesBeforeTitle);
        sb.Append(' ');
        sb.Append(reset);
        sb.Append(title);
        sb.Append(borderColor);
        sb.Append(' ');
        sb.Append(Horizontal, totalDashesAfterTitle);
        sb.Append(TopRight);
        sb.AppendLine(reset);
    }

    private static void AppendContentLine(StringBuilder sb, string line, int innerWidth, string borderColor, string reset)
    {
        var padding = innerWidth - line.Length - 2;

        sb.Append(borderColor);
        sb.Append(Vertical);
        sb.Append(reset);
        sb.Append(' ');
        sb.Append(line);
        sb.Append(' ', padding > 0 ? padding : 0);
        sb.Append(' ');
        sb.Append(borderColor);
        sb.Append(Vertical);
        sb.AppendLine(reset);
    }

    private static void AppendSeparator(StringBuilder sb, int innerWidth, string borderColor, string reset)
    {
        sb.Append(borderColor);
        sb.Append(SeparatorLeft);
        sb.Append(Horizontal, innerWidth);
        sb.Append(SeparatorRight);
        sb.AppendLine(reset);
    }

    private static void AppendBottomBorder(StringBuilder sb, int innerWidth, string borderColor, string reset)
    {
        sb.Append(borderColor);
        sb.Append(BottomLeft);
        sb.Append(Horizontal, innerWidth);
        sb.Append(BottomRight);
        sb.AppendLine(reset);
    }
}
