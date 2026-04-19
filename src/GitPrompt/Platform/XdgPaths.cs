namespace GitPrompt.Platform;

internal static class XdgPaths
{
    private const string AppName = "gitprompt";

    // Returns the platform config directory for gitprompt:
    //   Linux/macOS: $XDG_CONFIG_HOME/gitprompt  (fallback: ~/.config/gitprompt)
    //   Windows:     %APPDATA%\gitprompt
    internal static string GetConfigDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetEnvironmentVariable("APPDATA")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, AppName);
        }

        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrEmpty(xdgConfigHome))
        {
            return Path.Combine(xdgConfigHome, AppName);
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config", AppName);
    }

    // Returns the platform cache directory for gitprompt:
    //   Linux/macOS: $XDG_CACHE_HOME/gitprompt  (fallback: ~/.cache/gitprompt)
    //   Windows:     %LOCALAPPDATA%\gitprompt
    internal static string GetCacheDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, AppName);
        }

        var xdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (!string.IsNullOrEmpty(xdgCacheHome))
        {
            return Path.Combine(xdgCacheHome, AppName);
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".cache", AppName);
    }
}
