using System;
using System.Diagnostics;
using Microsoft.Win32;
using SpiderDesk.Core;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Uygulamanın Windows başlangıcında otomatik çalışması için Kayıt Defteri (Registry) işlemlerini yönetir.
    /// </summary>
    public class StartupRegistrationService : IStartupRegistrationService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SpiderDesk";

        public string? LastError { get; private set; }

        public bool IsEnabled()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                LastError = null;
                return key?.GetValue(AppName) != null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AppDiagnostics.LogWarning(nameof(StartupRegistrationService), $"Başlangıç kaydı okunamadı: {ex.Message}");
                return false;
            }
        }

        public void SetEnabled(bool enable)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null)
                {
                    LastError = $"Registry anahtarı açılamadı: {RunKeyPath}";
                    AppDiagnostics.LogWarning(nameof(StartupRegistrationService), LastError);
                    return;
                }

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

                LastError = null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AppDiagnostics.LogError(nameof(StartupRegistrationService), ex);
            }
        }
    }
}
