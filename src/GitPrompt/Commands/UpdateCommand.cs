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
            var process = Process.Start(psi);

            // On Windows the running .exe is locked and cannot be replaced while
            // we are still running. Exit immediately so the install script can
            // move the staged binary into place once the lock is released.
            if (OperatingSystem.IsWindows())
                return;

            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"gitprompt: update failed: {ex.Message}");
            Console.Error.WriteLine($"gitprompt: to update manually, run: curl -fsSL {InstallScriptUrl} | sh");
            Environment.Exit(1);
        }
    }
}
