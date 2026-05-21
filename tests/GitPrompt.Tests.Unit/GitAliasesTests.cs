using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace GitPrompt.Tests.Unit;

public sealed class GitAliasesTests
{
    private static readonly string AliasesFilePath = GetAliasesFilePath();

    private static string GetAliasesFilePath([CallerFilePath] string callerPath = "")
    {
        // Navigate from tests/GitPrompt.Tests.Unit/ up to the repo root
        var repoRoot = Path.GetFullPath(Path.Combine(callerPath, "..", "..", ".."));

        return Path.Combine(repoRoot, "git_aliases.sh");
    }

    private static IReadOnlyList<string> ParseAliasNames()
    {
        var content = File.ReadAllText(AliasesFilePath);
        var matches = Regex.Matches(content, @"^alias\s+(\w+)=", RegexOptions.Multiline);

        return matches.Select(match => match.Groups[1].Value).ToList();
    }

    private static IReadOnlyList<string> ParseFunctionNames()
    {
        var content = File.ReadAllText(AliasesFilePath);
        var matches = Regex.Matches(content, @"^(?:function\s+)?(\w+)\s*\(\)", RegexOptions.Multiline);

        return matches.Select(match => match.Groups[1].Value).ToList();
    }

    [Fact]
    public void AliasAndFunctionNames_WhenFileIsParsed_ShouldNotContainDuplicates()
    {
        // Arrange
        var allNames = ParseAliasNames().Concat(ParseFunctionNames()).ToList();

        // Act
        var duplicates = allNames
            .GroupBy(name => name)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        // Assert
        duplicates.Should().BeEmpty(
            because: $"every alias and function name must be unique, but found duplicates: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void AliasAndFunctionNames_WhenFileIsParsed_ShouldNotShadowWellKnownCommands()
    {
        // Arrange
        var wellKnownCommands = new HashSet<string>(StringComparer.Ordinal)
        {
            "ls", "cd", "cp", "mv", "rm", "mkdir", "rmdir", "touch",
            "cat", "echo", "printf", "read", "test", "true", "false",
            "grep", "find", "sed", "awk", "sort", "uniq", "cut", "head", "tail",
            "curl", "wget", "ssh", "scp", "rsync",
            "man", "which", "where", "type",
            "kill", "ps", "top", "bg", "fg", "jobs",
            "export", "source", "alias", "unalias", "set", "unset",
            "git", "sh", "bash", "zsh"
        };

        // Exclude private helpers (__-prefixed) from the shadowing check
        var publicNames = ParseAliasNames()
            .Concat(ParseFunctionNames().Where(n => !n.StartsWith("__", StringComparison.Ordinal)))
            .ToList();

        // Act
        var shadowedCommands = publicNames
            .Where(wellKnownCommands.Contains)
            .ToList();

        // Assert
        shadowedCommands.Should().BeEmpty(
            because: $"aliases and functions must not shadow well-known commands, but found: {string.Join(", ", shadowedCommands)}");
    }
}
