namespace GitPrompt;

internal static class AtomicFileWriter
{
    internal static void WriteAtomically(string targetFilePath, string content)
    {
        WriteAtomicallyCore(targetFilePath, tempFilePath => File.WriteAllText(tempFilePath, content));
    }

    internal static void WriteAtomically(string targetFilePath, string[] lines)
    {
        WriteAtomicallyCore(targetFilePath, tempFilePath => File.WriteAllLines(tempFilePath, lines));
    }

    private static void WriteAtomicallyCore(string targetFilePath, Action<string> writeTempFile)
    {
        var tempFilePath = targetFilePath + "." + Path.GetRandomFileName() + ".tmp";

        try
        {
            writeTempFile(tempFilePath);
            File.Move(tempFilePath, targetFilePath, overwrite: true);
            tempFilePath = null;
        }
        finally
        {
            if (tempFilePath is not null)
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception)
                {
                    /* best-effort temp file cleanup */
                }
            }
        }
    }
}
