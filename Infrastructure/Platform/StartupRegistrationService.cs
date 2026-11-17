using System;
using Microsoft.Win32;
using SpiderDesk.Core;

namespace SpiderDesk.Infrastructure.Platform
{
    /// <summary>
    /// Uygulamanın Windows başlangıcında otomatik çalışması için Kayıt Defteri (Registry) işlemlerini yönetir.
    /// Ne yapılacağına <see cref="StartupEntryPolicy"/> karar verir; bu sınıf yalnızca okur ve yazar.
    /// </summary>
    public class StartupRegistrationService : IStartupRegistrationService
    {
        private const string WindowsRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string DefaultEntryName = "SpiderDesk";

        private readonly string _runKeyPath;
        private readonly string _entryName;
        private readonly Func<string?> _exePath;

        public string? LastError { get; private set; }

        /// <summary>Uygulamanın kullandığı hâli: Windows'un gerçek "açılışta çalıştır" listesi ve çalışan exe.</summary>
        public StartupRegistrationService()
            : this(WindowsRunKeyPath, DefaultEntryName, () => Environment.ProcessPath)
        {
        }

        /// <summary>Testler gerçek listeye dokunmamak için kendi kayıt anahtarlarını ve sahte bir exe yolunu verir.</summary>
        public StartupRegistrationService(string runKeyPath, string entryName, Func<string?> exePath)
        {
            _runKeyPath = runKeyPath;
            _entryName = entryName;
            _exePath = exePath;
        }

        /// <summary>Kayıt varsa ve <b>bu exe'yi</b> gösteriyorsa true döner.</summary>
        public bool IsEnabled() => StartupEntryPolicy.PointsTo(ReadEntry(), _exePath());

        /// <summary>
        /// Kaydı ayara uydurur: açıksa kayıt bu exe'yi gösterecek şekilde yazılır (exe taşınmışsa düzeltilir),
        /// kapalıysa silinir. Kayıt zaten doğruysa hiçbir şey yazılmaz.
        /// </summary>
        public void SetEnabled(bool enable)
        {
            string? exePath = _exePath();
            StartupEntryAction action = StartupEntryPolicy.Decide(ReadEntry(), exePath, enable);
            if (action == StartupEntryAction.None) return;

            try
            {
                using RegistryKey? key = Registry.CurrentUser.CreateSubKey(_runKeyPath, writable: true);
                if (key == null)
                {
                    LastError = $"Registry anahtarı açılamadı: {_runKeyPath}";
                    AppDiagnostics.LogWarning(nameof(StartupRegistrationService), LastError);
                    return;
                }

                if (action == StartupEntryAction.Write)
                {
                    key.SetValue(_entryName, StartupEntryPolicy.EntryFor(exePath!));
                }
                else
                {
                    key.DeleteValue(_entryName, throwOnMissingValue: false);
                }

                LastError = null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AppDiagnostics.LogError(nameof(StartupRegistrationService), ex);
            }
        }

        private string? ReadEntry()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_runKeyPath, writable: false);
                LastError = null;
                return key?.GetValue(_entryName) as string;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AppDiagnostics.LogWarning(nameof(StartupRegistrationService), $"Başlangıç kaydı okunamadı: {ex.Message}");
                return null;
            }
        }
    }
}
