using System.IO;

namespace DisplayConfigManager.Services;

/// <summary>
/// Simple file-based diagnostic log so we can see what's happening
/// with auto-restore after sleep / unlock / display changes.
/// </summary>
public static class DiagnosticLog
{
    private static readonly string StorageDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DisplayConfigManager");

    public static readonly string LogPath = Path.Combine(StorageDir, "diagnostic.log");

    private static readonly object Sync = new();
    private const long MaxBytes = 512 * 1024; // 512 KB rolling

    public static void Write(string message)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(StorageDir);

                // Trivial rotation: if the file gets too big, archive it once.
                if (File.Exists(LogPath))
                {
                    var info = new FileInfo(LogPath);
                    if (info.Length > MaxBytes)
                    {
                        var backup = LogPath + ".1";
                        if (File.Exists(backup)) File.Delete(backup);
                        File.Move(LogPath, backup);
                    }
                }

                File.AppendAllText(
                    LogPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Never let logging itself crash the app.
        }
    }
}
