using GitPrompt.Configuration;
using GitPrompt.Platform;

namespace GitPrompt.Commands;

internal static class ConfigResetCommand
{
    internal static void Run(string? configPath = null, TextReader? input = null, TextWriter? output = null, bool skipConfirmation = false)
    {
        configPath ??= AppPaths.GetConfigFilePath();
        input ??= Console.In;
        output ??= Console.Out;

        if (!skipConfirmation)
        {
            output.Write("Reset config.jsonc to defaults? [y/N]: ");

            var answer = input.ReadLine();

            if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase))
            {
                output.WriteLine("Aborted.");
                return;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        File.WriteAllText(configPath, ConfigInitializer.BuildDefaultConfigContent());

        output.WriteLine("Config reset to defaults.");
    }
}
