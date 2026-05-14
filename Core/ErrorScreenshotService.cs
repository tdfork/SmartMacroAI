using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SmartMacroAI.Core;

/// <summary>
/// Automatically captures screenshots when errors occur during macro execution.
/// Saves PNG files to screenshots/errors/ with configurable retention limit.
/// </summary>
public sealed class ErrorScreenshotService
{
    private static readonly string ScreenshotDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "screenshots", "errors");

    /// <summary>Maximum number of error screenshots to retain. Oldest are deleted when exceeded.</summary>
    public int MaxScreenshots { get; set; } = 100;

    /// <summary>
    /// Captures a screenshot of the target window (or full screen as fallback) when an error occurs.
    /// Returns the saved file path, or null if capture failed.
    /// </summary>
    public string? CaptureOnError(IntPtr targetHwnd, string scriptName, string errorMessage, int actionIndex)
    {
        try
        {
            Directory.CreateDirectory(ScreenshotDir);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string safeName = SanitizeFileName(scriptName);
            string fileName = $"error_{safeName}_{timestamp}.png";
            string filePath = Path.Combine(ScreenshotDir, fileName);

            Bitmap? screenshot = null;

            // Try to capture target window first
            if (targetHwnd != IntPtr.Zero && Win32Api.IsWindow(targetHwnd))
            {
                try
                {
                    screenshot = CaptureWindow(targetHwnd);
                }
                catch
                {
                    screenshot = null;
                }
            }

            // Fallback: capture full screen
            screenshot ??= CaptureFullScreen();

            if (screenshot is null) return null;

            using (screenshot)
            {
                screenshot.Save(filePath, ImageFormat.Png);
            }

            // Write metadata sidecar
            string metaPath = Path.ChangeExtension(filePath, ".txt");
            string meta = $"""
                Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}
                Script: {scriptName}
                Action Index: {actionIndex}
                Error: {errorMessage}
                Target HWND: 0x{targetHwnd:X}
                Capture Mode: {(targetHwnd != IntPtr.Zero && Win32Api.IsWindow(targetHwnd) ? "Window" : "FullScreen")}
                """;
            File.WriteAllText(metaPath, meta);

            // Enforce retention limit
            EnforceRetentionLimit();

            return filePath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all error screenshot entries (newest first).
    /// </summary>
    public List<ErrorScreenshotEntry> GetAllEntries()
    {
        var entries = new List<ErrorScreenshotEntry>();
        if (!Directory.Exists(ScreenshotDir)) return entries;

        foreach (string pngFile in Directory.GetFiles(ScreenshotDir, "error_*.png"))
        {
            string metaFile = Path.ChangeExtension(pngFile, ".txt");
            string scriptName = "";
            string errorMessage = "";
            DateTime timestamp = File.GetCreationTime(pngFile);

            if (File.Exists(metaFile))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(metaFile))
                    {
                        if (line.StartsWith("Script: "))
                            scriptName = line["Script: ".Length..];
                        else if (line.StartsWith("Error: "))
                            errorMessage = line["Error: ".Length..];
                        else if (line.StartsWith("Timestamp: ") && DateTime.TryParse(line["Timestamp: ".Length..], out var ts))
                            timestamp = ts;
                    }
                }
                catch { }
            }

            entries.Add(new ErrorScreenshotEntry
            {
                FilePath = pngFile,
                ScriptName = scriptName,
                ErrorMessage = errorMessage,
                Timestamp = timestamp,
            });
        }

        return entries.OrderByDescending(e => e.Timestamp).ToList();
    }

    private void EnforceRetentionLimit()
    {
        if (!Directory.Exists(ScreenshotDir)) return;

        var files = Directory.GetFiles(ScreenshotDir, "error_*.png")
            .OrderByDescending(f => File.GetCreationTime(f))
            .ToList();

        if (files.Count <= MaxScreenshots) return;

        foreach (string file in files.Skip(MaxScreenshots))
        {
            try
            {
                File.Delete(file);
                string meta = Path.ChangeExtension(file, ".txt");
                if (File.Exists(meta)) File.Delete(meta);
            }
            catch { }
        }
    }

    private static Bitmap? CaptureWindow(IntPtr hwnd)
    {
        if (!Win32Api.GetWindowRect(hwnd, out var rect)) return null;
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return null;

        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        IntPtr hdc = g.GetHdc();
        Win32Api.PrintWindow(hwnd, hdc, 2); // PW_RENDERFULLCONTENT
        g.ReleaseHdc(hdc);
        return bmp;
    }

    private static Bitmap CaptureFullScreen()
    {
        int screenW = System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Width;
        int screenH = System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Height;
        var bmp = new Bitmap(screenW, screenH, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(0, 0, 0, 0, new Size(screenW, screenH));
        return bmp;
    }

    private static string SanitizeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string safe = new(name.Where(c => !invalid.Contains(c)).ToArray());
        return safe.Length > 30 ? safe[..30] : (safe.Length == 0 ? "unknown" : safe);
    }
}

/// <summary>
/// Represents a single error screenshot entry for display in Run History.
/// </summary>
public class ErrorScreenshotEntry
{
    public string FilePath { get; set; } = "";
    public string ScriptName { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string FileName => Path.GetFileName(FilePath);
}
