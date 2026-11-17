using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Spider.Themes;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Tepsideki kayıtlı ayarlar: tema, gece/gündüz, hız ve sıklık alt menüleri ile
    /// "Pencerelerin Üstünde Göster" ve "Otomatik Başlat" seçenekleri. Bu öğelere tıklamak menüyü kapatmaz.
    /// </summary>
    public sealed class SettingsMenu
    {
        public event Action<ISpiderTheme>? ThemeSelected;
        public event Action<EnvironmentTimeMode>? TimeModeSelected;
        public event Action<BugSpawnRatePreset>? SpawnRateSelected;
        public event Action<DewSpawnRatePreset>? DewSpawnRateSelected;
        public event Action<SpiderSpeedPreset>? SpeedPresetSelected;
        public event Action<bool>? AlwaysOnTopChanged;
        public event Action<bool>? StartupChanged;

        /// <summary>Ana menüye sırayla eklenecek öğeler.</summary>
        public IReadOnlyList<ToolStripItem> Items { get; }

        public SettingsMenu(UserSettings settings, bool startupEnabled, KeepMenuOpen keepOpen)
        {
            Items = new ToolStripItem[]
            {
                BuildThemeMenu(settings.ThemeId, keepOpen),
                BuildTimeModeMenu(settings.TimeMode, keepOpen),
                BuildSpeedAndRatesMenu(settings, keepOpen),
                new ToolStripSeparator(),
                Toggle("🖥️ Pencerelerin Üstünde Göster", settings.AlwaysOnTop, isOn => AlwaysOnTopChanged?.Invoke(isOn), keepOpen),
                Toggle("🚀 Bilgisayar Açıldığında Otomatik Başlat", startupEnabled, isOn => StartupChanged?.Invoke(isOn), keepOpen)
            };
        }

        private ToolStripMenuItem BuildThemeMenu(string currentThemeId, KeepMenuOpen keepOpen)
        {
            var root = new ToolStripMenuItem("🎨 Örümcek Teması");
            TrayMenuItems.AddRadioOptions(
                root,
                SpiderThemeCatalog.All.Select(theme => (theme.DisplayName, theme)),
                theme => theme.Id == currentThemeId,
                theme => ThemeSelected?.Invoke(theme),
                keepOpen);

            keepOpen.AttachTo(root.DropDown);
            return root;
        }

        private ToolStripMenuItem BuildTimeModeMenu(EnvironmentTimeMode currentMode, KeepMenuOpen keepOpen)
        {
            var root = new ToolStripMenuItem("🌙 Gece/Gündüz Döngüsü");
            TrayMenuItems.AddRadioOptions(
                root,
                new (string, EnvironmentTimeMode)[]
                {
                    ("Otomatik (Sistem Saati)", EnvironmentTimeMode.AutoBySystemClock),
                    ("Sürekli Gündüz", EnvironmentTimeMode.AlwaysDay),
                    ("Sürekli Gece (Ateş Böcekleri)", EnvironmentTimeMode.AlwaysNight)
                },
                mode => mode == currentMode,
                mode => TimeModeSelected?.Invoke(mode),
                keepOpen);

            keepOpen.AttachTo(root.DropDown);
            return root;
        }

        private ToolStripMenuItem BuildSpeedAndRatesMenu(UserSettings settings, KeepMenuOpen keepOpen)
        {
            var root = new ToolStripMenuItem("⚙️ Hız ve Sıklık Ayarları");

            root.DropDownItems.Add(TrayMenuItems.Header("🐛 Böcek Çıkma Sıklığı"));
            TrayMenuItems.AddRadioOptions(
                root,
                new (string, BugSpawnRatePreset)[]
                {
                    ("Seyrek (45-75 sn)", BugSpawnRatePreset.Low),
                    ("Normal (20-38 sn)", BugSpawnRatePreset.Normal),
                    ("Yoğun (8-17 sn)", BugSpawnRatePreset.High)
                },
                preset => settings.SpawnRate == preset,
                preset => SpawnRateSelected?.Invoke(preset),
                keepOpen);

            root.DropDownItems.Add(new ToolStripSeparator());

            root.DropDownItems.Add(TrayMenuItems.Header("💧 Çiy Oluşma Sıklığı"));
            TrayMenuItems.AddRadioOptions(
                root,
                new (string, DewSpawnRatePreset)[]
                {
                    ("Kapalı", DewSpawnRatePreset.Off),
                    ("Seyrek (30-50 sn)", DewSpawnRatePreset.Low),
                    ("Normal (14-26 sn)", DewSpawnRatePreset.Normal),
                    ("Yoğun (5-10 sn)", DewSpawnRatePreset.High)
                },
                preset => settings.DewSpawnRate == preset,
                preset => DewSpawnRateSelected?.Invoke(preset),
                keepOpen);

            root.DropDownItems.Add(new ToolStripSeparator());

            root.DropDownItems.Add(TrayMenuItems.Header("⚡ Örümcek Hızı"));
            TrayMenuItems.AddRadioOptions(
                root,
                new (string, SpiderSpeedPreset)[]
                {
                    ("Sakin (0.75x)", SpiderSpeedPreset.Calm),
                    ("Normal (1.0x)", SpiderSpeedPreset.Normal),
                    ("Çevik (1.35x)", SpiderSpeedPreset.Agile)
                },
                preset => settings.SpeedPreset == preset,
                preset => SpeedPresetSelected?.Invoke(preset),
                keepOpen);

            keepOpen.AttachTo(root.DropDown);
            return root;
        }

        /// <summary>Tıklayınca açılıp kapanan (✓) seçenek.</summary>
        private static ToolStripMenuItem Toggle(string text, bool isOn, Action<bool> onChanged, KeepMenuOpen keepOpen)
        {
            var item = keepOpen.Mark(new ToolStripMenuItem(text) { Checked = isOn, CheckOnClick = true });
            item.CheckedChanged += (_, _) => onChanged(item.Checked);
            return item;
        }
    }
}
