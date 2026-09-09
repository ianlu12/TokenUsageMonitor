using System.IO;
using System.Runtime.InteropServices;

namespace TokenUsageMonitor.Services;

/// <summary>
/// Manages auto-start via a .lnk shortcut in the per-user Startup folder
/// (no admin rights required, visible/disableable in Task Manager).
/// </summary>
public sealed class StartupManager
{
    private const string ShortcutName = "TokenUsageMonitor.lnk";

    private static string StartupDirectory
        => Environment.GetFolderPath(Environment.SpecialFolder.Startup);

    private string ShortcutPath => Path.Combine(StartupDirectory, ShortcutName);

    public bool IsEnabled() => File.Exists(ShortcutPath);

    public void Enable()
    {
        string target = Environment.ProcessPath
                        ?? throw new InvalidOperationException("無法判斷應用程式執行檔路徑。");

        Directory.CreateDirectory(StartupDirectory);

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
                        ?? throw new InvalidOperationException("無法建立 WScript.Shell COM 物件。");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(ShortcutPath);
        shortcut.TargetPath = target;
        shortcut.WorkingDirectory = Path.GetDirectoryName(target) ?? string.Empty;
        shortcut.Description = "Token Usage Monitor - AI 用量監控";
        shortcut.Save();
        Marshal.FinalReleaseComObject(shortcut);
        Marshal.FinalReleaseComObject(shell);
    }

    public void Disable()
    {
        try
        {
            if (File.Exists(ShortcutPath))
            {
                File.Delete(ShortcutPath);
            }
        }
        catch (IOException)
        {
            // Best effort; if deletion fails the shortcut simply remains.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}