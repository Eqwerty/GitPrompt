using GitPrompt.Platform;

namespace GitPrompt.Configuration;

internal static class ConfigInitializer
{
    internal static void InitializeDefaultConfig()
    {
        try
        {
            var configDirectory = XdgPaths.GetConfigDirectory();
            var configFile = Path.Combine(configDirectory, "config.json");

            if (File.Exists(configFile))
            {
                return;
            }

            Directory.CreateDirectory(configDirectory);

            using var stream = typeof(ConfigInitializer).Assembly.GetManifestResourceStream("default-config.json")!;

            using var fileStream = File.Create(configFile);
            stream.CopyTo(fileStream);
        }
        catch
        {
            // Non-critical: the binary works fine with default settings even when the config file is absent.
        }
    }
}
