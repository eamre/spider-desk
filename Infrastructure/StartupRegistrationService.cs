using System.Diagnostics;
using Microsoft.Win32;
using SpiderDesk.Core;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Uygulamanın Windows başlangıcında otomatik çalışması için Kayıt Defteri (Registry) işlemlerini yönetir (SRP & DIP).
    /// </summary>
    public class StartupRegistrationService : IStartupRegistrationService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SpiderDesk";

        public bool IsEnabled()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public void SetEnabled(bool enable)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null) return;

                if (enable)
                {
                    string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch
            {
                // Kayıt defteri erişim kısıtlamalarını sessizce geç
            }
        }
    }
}
