using static Prompt.Constants.PromptColors;

namespace Prompt;

internal static class PromptContextBuilder
{
    internal static string Build()
    {
        var user = Environment.GetEnvironmentVariable("USER");
        var windowsUserName = Environment.GetEnvironmentVariable("USERNAME");
        var host = Environment.MachineName;

        string workingDirectoryPath;
        try
        {
            workingDirectoryPath = Directory.GetCurrentDirectory();
        }
        catch
        {
            workingDirectoryPath = "?";
        }

        var homeDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return Build(
            user,
            windowsUserName,
            host,
            workingDirectoryPath,
            homeDirectoryPath,
            OperatingSystem.IsWindows());
    }

    internal static string Build(
        string? user,
        string? windowsUserName,
        string? host,
        string? workingDirectoryPath,
        string? homeDirectoryPath,
        bool isWindows)
    {
        var resolvedUser = ResolveUser(user, windowsUserName);
        var resolvedHost = ResolveHost(host);
        var resolvedPath = ResolveWorkingDirectoryPath(workingDirectoryPath, homeDirectoryPath, isWindows);

        return $"{ColorUser}{resolvedUser}{ColorReset} {ColorHost}{resolvedHost}{ColorReset} {ColorPath}{resolvedPath}{ColorReset}";
    }

    private static string ResolveUser(string? user, string? windowsUserName)
    {
        if (!string.IsNullOrEmpty(user))
        {
            return user;
        }

        if (!string.IsNullOrEmpty(windowsUserName))
        {
            return windowsUserName;
        }

        return "?";
    }

    private static string ResolveHost(string? host)
    {
        if (string.IsNullOrEmpty(host))
        {
            return "?";
        }

        var dotIndex = host.IndexOf('.');
        return dotIndex > 0 ? host[..dotIndex] : host;
    }

    private static string ResolveWorkingDirectoryPath(string? workingDirectoryPath, string? homeDirectoryPath, bool isWindows)
    {
        var resolvedPath = string.IsNullOrEmpty(workingDirectoryPath) ? "?" : workingDirectoryPath;

        if (resolvedPath is "?")
        {
            return resolvedPath;
        }

        try
        {
            if (!string.IsNullOrEmpty(homeDirectoryPath))
            {
                var fullWorkingDirectoryPath = Path.GetFullPath(resolvedPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var fullHomeDirectoryPath = Path.GetFullPath(homeDirectoryPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var pathComparison = isWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                if (string.Equals(fullWorkingDirectoryPath, fullHomeDirectoryPath, pathComparison))
                {
                    resolvedPath = "~";
                }
                else if (fullWorkingDirectoryPath.StartsWith(fullHomeDirectoryPath + Path.DirectorySeparatorChar, pathComparison))
                {
                    resolvedPath = "~" + fullWorkingDirectoryPath[fullHomeDirectoryPath.Length..];
                }
            }
        }
        catch
        {
            // Keep the raw path if normalization fails.
        }

        return resolvedPath.Replace('\\', '/');
    }
}


