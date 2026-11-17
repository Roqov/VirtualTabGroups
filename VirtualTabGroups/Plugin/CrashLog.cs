using System;
using System.IO;
using System.Text;

namespace VirtualTabGroups.Plugin
{
    /// <summary>
    /// Best-effort exception logger writing to %appdata%\Notepad++\plugins\config\VirtualTabGroups\plugin.log.
    /// Every Write call is wrapped in its own try/catch so logging failures never throw.
    /// The file is capped: once it grows past 1 MB it is moved to plugin.log.1 and a
    /// fresh log is started, so a long-lived install does not accumulate forever.
    /// </summary>
    internal static class CrashLog
    {
        private const long MaxLogBytes = 1024 * 1024;
        private const int SizeCheckInterval = 200;

        private static string _logPath;
        private static int _writesSinceSizeCheck;

        public static string LogPath => _logPath;

        public static void Initialize(string pluginConfigDir)
        {
            try
            {
                Directory.CreateDirectory(pluginConfigDir);
                _logPath = Path.Combine(pluginConfigDir, "plugin.log");
                RotateIfNeeded();
                Write("=== Plugin session started at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    + " | plugin " + typeof(CrashLog).Assembly.GetName().Version
                    + " | " + Environment.OSVersion.VersionString + " ===");
            }
            catch { _logPath = null; }
        }

        public static void Write(string line)
        {
            if (_logPath == null) return;
            try
            {
                File.AppendAllText(_logPath, "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + line + Environment.NewLine, Encoding.UTF8);

                // Stat the file only every so often: append-only logging should stay cheap.
                if (++_writesSinceSizeCheck >= SizeCheckInterval)
                {
                    _writesSinceSizeCheck = 0;
                    RotateIfNeeded();
                }
            }
            catch { /* swallow */ }
        }

        /// <summary>Returns the last <paramref name="maxLines"/> lines, or "" when unreadable.</summary>
        public static string Tail(int maxLines)
        {
            if (_logPath == null || maxLines <= 0) return string.Empty;
            try
            {
                if (!File.Exists(_logPath)) return string.Empty;

                var lines = File.ReadAllLines(_logPath);
                int start = Math.Max(0, lines.Length - maxLines);
                var sb = new StringBuilder();
                for (int i = start; i < lines.Length; i++) sb.AppendLine(lines[i]);
                return sb.ToString();
            }
            catch { return string.Empty; }
        }

        private static void RotateIfNeeded()
        {
            if (_logPath == null) return;
            try
            {
                var info = new FileInfo(_logPath);
                if (!info.Exists || info.Length <= MaxLogBytes) return;

                var previous = _logPath + ".1";
                if (File.Exists(previous)) File.Delete(previous);
                File.Move(_logPath, previous);
            }
            catch { /* rotation is best-effort; logging continues in the same file */ }
        }

        public static void WriteException(string source, Exception ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("EXCEPTION in " + source + ":");
                sb.AppendLine("  Type: " + ex.GetType().FullName);
                sb.AppendLine("  Message: " + ex.Message);
                sb.AppendLine("  Stack:");
                foreach (var line in (ex.StackTrace ?? string.Empty).Split('\n'))
                    sb.AppendLine("    " + line.TrimEnd('\r'));
                var inner = ex.InnerException;
                while (inner != null)
                {
                    sb.AppendLine("  Caused by " + inner.GetType().FullName + ": " + inner.Message);
                    foreach (var line in (inner.StackTrace ?? string.Empty).Split('\n'))
                        sb.AppendLine("    " + line.TrimEnd('\r'));
                    inner = inner.InnerException;
                }
                Write(sb.ToString());
            }
            catch { /* swallow */ }
        }
    }
}
