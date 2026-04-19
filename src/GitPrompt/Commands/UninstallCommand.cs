using System.Diagnostics;

namespace GitPrompt.Commands;

internal static class UninstallCommand
{
    private const string UninstallScriptUrl =
        "https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/uninstall.sh";

    internal static void Run()
    {
        var sslOpt = OperatingSystem.IsWindows() ? "--ssl-no-revoke " : "";
        var script = $"curl -fsSL {sslOpt}{UninstallScriptUrl} | sh";

        try
        {
            var psi = new ProcessStartInfo("sh") { UseShellExecute = false };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(script);
            var process = Process.Start(psi);

            // On Windows the running .exe is locked, so we must exit before the
            // uninstall script tries to delete the binary. The sh process keeps
            // running after we exit and can then remove the now-unlocked file.
            if (OperatingSystem.IsWindows())
                return;

            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"gitprompt: uninstall failed: {ex.Message}");
            Console.Error.WriteLine($"gitprompt: to uninstall manually, run: curl -fsSL {UninstallScriptUrl} | sh");
            Environment.Exit(1);
        }
    }
}
