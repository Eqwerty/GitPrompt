namespace GitPrompt.Platform;

internal static class AppPaths
{
    internal static string? GetBinaryPath()
    {
        return Environment.ProcessPath;
    }

    internal static string GetConfigFilePath()
    {
        return Path.Combine(XdgPaths.GetConfigDirectory(), "config.jsonc");
    }

    internal static string GetAliasesFilePath()
    {
        return Path.Combine(XdgPaths.GetDataDirectory(), "git_aliases.sh");
    }

    internal static string GetCacheDirectory()
    {
        return XdgPaths.GetCacheDirectory();
    }

    internal static string? GetManPagePath()
    {
        if (OperatingSystem.IsWindows())
        {
            return null;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return Path.Combine(home, ".local", "share", "man", "man1", "gitprompt.1");
    }
}
