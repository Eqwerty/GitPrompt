using System.Diagnostics;

namespace GitPrompt.Commands;

internal static class UpdateCommand
{
    private const string InstallScriptUrl =
        "https://raw.githubusercontent.com/Eqwerty/GitPrompt/master/install.sh";

    internal static void Run()
    {
        var sslOpt = OperatingSystem.IsWindows() ? "--ssl-no-revoke " : "";
        var script = $"curl -fsSL {sslOpt}{InstallScriptUrl} | sh";

        try
        {
            var psi = new ProcessStartInfo("sh") { UseShellExecute = false };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(script);
            Process.Start(psi)?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"gitprompt: update failed: {ex.Message}");
            Console.Error.WriteLine($"gitprompt: to update manually, run: curl -fsSL {InstallScriptUrl} | sh");
            Environment.Exit(1);
        }
    }
}
