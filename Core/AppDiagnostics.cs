using System;
using System.Diagnostics;
using System.IO;

namespace SpiderDesk.Core
{
    /// <summary>
    /// Uygulama genelinde uyarı, hata ve beklenmeyen istisnaları hem Trace kanalına
    /// hem de %LocalAppData%\SpiderDesk\diagnostics.log dosyasına kaydeden merkezi tanılama yardımcısı.
    /// </summary>
    public static class AppDiagnostics
    {
        private static readonly object SyncLock = new();
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpiderDesk",
            "diagnostics.log"
        );
        private const long MaxLogFileBytes = 256 * 1024; // 256 KB üst sınır

        public static void LogWarning(string context, string message)
        {
            string formatted = $"[SpiderDesk][WARN][{context}] {message}";
            Trace.TraceWarning(formatted);
            AppendToFile(formatted);
        }

        public static void LogError(string context, Exception ex)
        {
            string formatted = $"[SpiderDesk][ERROR][{context}] {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
            Trace.TraceError(formatted);
            AppendToFile(formatted);
        }

        private static void AppendToFile(string message)
        {
            try
            {
                lock (SyncLock)
                {
                    string? dir = Path.GetDirectoryName(LogFilePath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxLogFileBytes)
                    {
                        File.WriteAllText(LogFilePath, string.Empty);
                    }

                    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
                    File.AppendAllText(LogFilePath, line);
                }
            }
            catch
            {
                // Log dosyasına erişilemezse uygulamanın çalışmasını engelleme
            }
        }
    }
}
