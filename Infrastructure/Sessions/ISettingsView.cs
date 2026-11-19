using System;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Spider.Themes;

namespace SpiderDesk.Infrastructure.Sessions
{
    /// <summary>
    /// Kayıtlı ayarların gösterildiği ve değiştirildiği yer (ayarlar penceresinin "Ayarlar" sekmesi).
    /// <see cref="SettingsSessionController"/> yalnızca bu arayüzü tanır; testlerde yerine sahte bir görünüm verilir.
    /// </summary>
    public interface ISettingsView
    {
        event Action<ISpiderTheme>? ThemeSelected;
        event Action<EnvironmentTimeMode>? TimeModeSelected;
        event Action<SpiderSpeedPreset>? SpeedPresetSelected;
        event Action<BugSpawnRatePreset>? SpawnRateSelected;
        event Action<DewSpawnRatePreset>? DewSpawnRateSelected;
        event Action<bool>? AlwaysOnTopChanged;
        event Action<bool>? StartupChanged;

        /// <summary>Ayarların o anki değerlerini gösterir. Bu çağrı yukarıdaki olayları tetiklemez.</summary>
        void ShowSettings(UserSettings settings, bool runsOnStartup);

        /// <summary>Son kayıt başarısız olduysa uyarıyı gösterir, başarılı olduysa gizler.</summary>
        void ShowSaveFailed(bool failed);
    }
}
