using System.Diagnostics;
using System.Text.RegularExpressions;
using GitPrompt.Platform;

namespace GitPrompt.Commands;

internal static class AliasesCommand
{
    internal static void Run(string? aliasesPath = null, TextWriter? errorOutput = null)
    {
        errorOutput ??= Console.Error;

        if (!TryResolveAliasesFile(aliasesPath, errorOutput, out var resolvedAliasesPath))
        {
            return;
        }

        aliasesPath = resolvedAliasesPath;

        var editor = EditorResolver.GetEditor();

        try
        {
            var processStartInfo = new ProcessStartInfo(editor) { UseShellExecute = false };
            processStartInfo.ArgumentList.Add(aliasesPath);

            Process.Start(processStartInfo)?.WaitForExit();
        }
        catch (Exception exception)
        {
            errorOutput.WriteLine($"gitprompt: failed to open editor '{editor}': {exception.Message}");
            errorOutput.WriteLine($"gitprompt: aliases file is at: {aliasesPath}");

            Environment.Exit(1);
        }
    }

    internal static void RunEnable(string? aliasesPath = null, TextWriter? scriptOutput = null, TextWriter? errorOutput = null)
    {
        scriptOutput ??= Console.Out;
        errorOutput ??= Console.Error;

        if (!TryResolveAliasesFile(aliasesPath, errorOutput, out var resolvedAliasesPath))
        {
            return;
        }

        aliasesPath = resolvedAliasesPath;

        var escapedPath = aliasesPath.Replace("'", "'\\''");
        scriptOutput.WriteLine($". '{escapedPath}'");
        scriptOutput.WriteLine("_GITPROMPT_ALIASES_ENABLED=1");
    }

    internal static void RunDisable(string? aliasesPath = null, TextWriter? scriptOutput = null, TextWriter? errorOutput = null)
    {
        scriptOutput ??= Console.Out;
        errorOutput ??= Console.Error;

        if (!TryResolveAliasesFile(aliasesPath, errorOutput, out var resolvedAliasesPath))
        {
            return;
        }

        aliasesPath = resolvedAliasesPath;

        var content = File.ReadAllText(aliasesPath);

        var aliasNames = Regex.Matches(content, @"^alias\s+(\w+)=", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value)
            .ToList();

        var functionNames = Regex.Matches(content, @"^function\s+(\w+)\s*\(", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value)
            .ToList();

        if (aliasNames.Count > 0)
        {
            scriptOutput.WriteLine($"unalias {string.Join(" ", aliasNames)} 2>/dev/null || true");
        }

        if (functionNames.Count > 0)
        {
            scriptOutput.WriteLine($"unset -f {string.Join(" ", functionNames)} 2>/dev/null || true");
        }

        scriptOutput.WriteLine("_GITPROMPT_ALIASES_ENABLED=0");
    }

    private static bool TryResolveAliasesFile(string? aliasesPath, TextWriter errorOutput, out string resolvedAliasesPath)
    {
        resolvedAliasesPath = aliasesPath ?? AppPaths.GetAliasesFilePath();

        if (File.Exists(resolvedAliasesPath))
        {
            return true;
        }

        errorOutput.WriteLine($"gitprompt: git aliases not found at: {resolvedAliasesPath}");
        errorOutput.WriteLine("gitprompt: run 'gitprompt update aliases' to install them");

        return false;
    }
}
