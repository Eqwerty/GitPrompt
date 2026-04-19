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
            Process.Start(psi)?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"gitprompt: uninstall failed: {ex.Message}");
            Console.Error.WriteLine($"gitprompt: to uninstall manually, run: curl -fsSL {UninstallScriptUrl} | sh");
            Environment.Exit(1);
        }
    }
}
