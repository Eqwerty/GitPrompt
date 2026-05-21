using System.Reflection;

namespace GitPrompt.Commands;

internal static class VersionCommand
{
    internal static void PrintVersion(TextWriter? output = null)
    {
        output ??= Console.Out;

        var informationalVersion = typeof(VersionCommand).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        output.WriteLine(GetVersion(informationalVersion));
    }

    internal static string GetVersion(string? informationalVersion)
    {
        return string.IsNullOrWhiteSpace(informationalVersion) ? "unknown" : informationalVersion;
    }
}
