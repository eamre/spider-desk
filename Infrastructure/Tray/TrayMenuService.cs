using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Spider.Themes;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Sistem tepsisi (System Tray) ikonunu, tema seçicisini ve ayar menülerini yönetir.
    /// </summary>
    public sealed class TrayMenuService : IDisposable
    {
        // Bu etiketi taşıyan öğelere (ayar seçimleri) tıklanınca menü açık kalır; eylemler menüyü kapatır
        private static readonly object KeepMenuOpenTag = new();

        private readonly NotifyIcon _notifyIcon;
        private readonly ToolStripMenuItem _statusMenuItem;
        private string _lastStatusText = string.Empty;
        private bool _lastClickKeepsMenuOpen;

        public event Action? NewWebRequested;
        public event Action? RappelRequested;
        public event Action? RainOnWebsRequested;
        public event Action<int>? SpawnBugsRequested;
        public event Action? ClearWebsRequested;
        public event Action<ISpiderTheme>? ThemeSelected;
        public event Action<BugSpawnRatePreset>? SpawnRateSelected;
        public event Action<DewSpawnRatePreset>? DewSpawnRateSelected;
        public event Action<SpiderSpeedPreset>? SpeedPresetSelected;
        public event Action<EnvironmentTimeMode>? TimeModeSelected;
        public event Action<bool>? AlwaysOnTopChanged;
        public event Action<bool>? StartupChanged;
        public event Action<bool>? PauseChanged;
        public event Action? ExitRequested;

        public TrayMenuService(UserSettings currentSettings, bool initialStartupEnabled)
        {
            var menu = new ContextMenuStrip();

            _statusMenuItem = new ToolStripMenuItem("🕷️ Seviye 1 | Başlatılıyor...")
            {
                Enabled = false
            };
            menu.Items.Add(_statusMenuItem);
            menu.Items.Add(new ToolStripSeparator());

            AddActionItem(menu, "🕸️ Hemen Yeni Ağ Ör", () => NewWebRequested?.Invoke());
            AddActionItem(menu, "🧗 Tavandan İple Sark", () => RappelRequested?.Invoke());
            AddActionItem(menu, "💧 Ağlara Çiy/Su Damlası Bırak", () => RainOnWebsRequested?.Invoke());
            AddActionItem(menu, "🐛 Ekrana Böcek Gönder (3 Adet)", () => SpawnBugsRequested?.Invoke(3));
            AddActionItem(menu, "🧹 Ağları Temizle", () => ClearWebsRequested?.Invoke());

            menu.Items.Add(new ToolStripSeparator());

            var themeMenu = BuildThemeSubMenu(currentSettings.ThemeId);
            var timeModeMenu = BuildTimeModeSubMenu(currentSettings.TimeMode);
            var simulationMenu = BuildSimulationSettingsSubMenu(currentSettings);
            menu.Items.Add(themeMenu);
            menu.Items.Add(timeModeMenu);
            menu.Items.Add(simulationMenu);

            menu.Items.Add(new ToolStripSeparator());

            var alwaysOnTopItem = new ToolStripMenuItem("🖥️ Pencerelerin Üstünde Göster")
            {
                Checked = currentSettings.AlwaysOnTop,
                CheckOnClick = true,
                Tag = KeepMenuOpenTag
            };
            alwaysOnTopItem.CheckedChanged += (_, _) => AlwaysOnTopChanged?.Invoke(alwaysOnTopItem.Checked);
            menu.Items.Add(alwaysOnTopItem);

            var startupItem = new ToolStripMenuItem("🚀 Bilgisayar Açıldığında Otomatik Başlat")
            {
                Checked = initialStartupEnabled,
                CheckOnClick = true,
                Tag = KeepMenuOpenTag
            };
            startupItem.CheckedChanged += (_, _) => StartupChanged?.Invoke(startupItem.Checked);
            menu.Items.Add(startupItem);

            var pauseItem = new ToolStripMenuItem("⏸️ Duraklat")
            {
                CheckOnClick = true
            };
            pauseItem.CheckedChanged += (_, _) =>
            {
                bool paused = pauseItem.Checked;
                pauseItem.Text = paused ? "▶️ Devam Et" : "⏸️ Duraklat";
                PauseChanged?.Invoke(paused);
            };
            menu.Items.Add(pauseItem);

            menu.Items.Add(new ToolStripSeparator());
            AddActionItem(menu, "❌ Çıkış", () => ExitRequested?.Invoke());

            KeepOpenOnSettingClicks(menu);
            KeepOpenOnSettingClicks(themeMenu.DropDown);
            KeepOpenOnSettingClicks(timeModeMenu.DropDown);
            KeepOpenOnSettingClicks(simulationMenu.DropDown);

            _notifyIcon = new NotifyIcon
            {
                Icon = TrayIconFactory.CreateSpiderIcon(),
                Text = "SpiderDesk - Masaüstü Örümceği",
                ContextMenuStrip = menu,
                Visible = true
            };

            _notifyIcon.DoubleClick += (_, _) => SpawnBugsRequested?.Invoke(2);
        }

        private ToolStripMenuItem BuildThemeSubMenu(string currentThemeId)
        {
            var themeRoot = new ToolStripMenuItem("🎨 Örümcek Teması");
            var options = new List<(string Label, ISpiderTheme Value)>();
            foreach (var theme in SpiderThemeCatalog.All)
            {
                options.Add((theme.DisplayName, theme));
            }

            AddRadioOptions(
                themeRoot,
                options,
                t => t.Id == currentThemeId,
                t => ThemeSelected?.Invoke(t)
            );
            return themeRoot;
        }

        private ToolStripMenuItem BuildTimeModeSubMenu(EnvironmentTimeMode currentMode)
        {
            var timeRoot = new ToolStripMenuItem("🌙 Gece/Gündüz Döngüsü");
            var options = new (string Label, EnvironmentTimeMode Mode)[]
            {
                ("Otomatik (Sistem Saati)", EnvironmentTimeMode.AutoBySystemClock),
                ("Sürekli Gündüz", EnvironmentTimeMode.AlwaysDay),
                ("Sürekli Gece (Ateş Böcekleri)", EnvironmentTimeMode.AlwaysNight)
            };

            AddRadioOptions(
                timeRoot,
                options,
                m => m == currentMode,
                m => TimeModeSelected?.Invoke(m)
            );
            return timeRoot;
        }

        private ToolStripMenuItem BuildSimulationSettingsSubMenu(UserSettings settings)
        {
            var settingsRoot = new ToolStripMenuItem("⚙️ Hız ve Sıklık Ayarları");

            settingsRoot.DropDownItems.Add(new ToolStripMenuItem("🐛 Böcek Çıkma Sıklığı") { Enabled = false });
            AddRadioOptions(
                settingsRoot,
                new (string, BugSpawnRatePreset)[]
                {
                    ("Seyrek (45-75 sn)", BugSpawnRatePreset.Low),
                    ("Normal (20-38 sn)", BugSpawnRatePreset.Normal),
                    ("Yoğun (8-17 sn)", BugSpawnRatePreset.High)
                },
                preset => settings.SpawnRate == preset,
                preset => SpawnRateSelected?.Invoke(preset)
            );

            settingsRoot.DropDownItems.Add(new ToolStripSeparator());

            settingsRoot.DropDownItems.Add(new ToolStripMenuItem("💧 Çiy Oluşma Sıklığı") { Enabled = false });
            AddRadioOptions(
                settingsRoot,
                new (string, DewSpawnRatePreset)[]
                {
                    ("Kapalı", DewSpawnRatePreset.Off),
                    ("Seyrek (30-50 sn)", DewSpawnRatePreset.Low),
                    ("Normal (14-26 sn)", DewSpawnRatePreset.Normal),
                    ("Yoğun (5-10 sn)", DewSpawnRatePreset.High)
                },
                preset => settings.DewSpawnRate == preset,
                preset => DewSpawnRateSelected?.Invoke(preset)
            );

            settingsRoot.DropDownItems.Add(new ToolStripSeparator());

            settingsRoot.DropDownItems.Add(new ToolStripMenuItem("⚡ Örümcek Hızı") { Enabled = false });
            AddRadioOptions(
                settingsRoot,
                new (string, SpiderSpeedPreset)[]
                {
                    ("Sakin (0.75x)", SpiderSpeedPreset.Calm),
                    ("Normal (1.0x)", SpiderSpeedPreset.Normal),
                    ("Çevik (1.35x)", SpiderSpeedPreset.Agile)
                },
                preset => settings.SpeedPreset == preset,
                preset => SpeedPresetSelected?.Invoke(preset)
            );

            return settingsRoot;
        }

        /// <summary>
        /// Windows menüleri her tıklamada kapanır. Tıklanan öğe bir ayar seçimiyse bu kapanmayı iptal eder,
        /// böylece kullanıcı birkaç ayarı menüyü yeniden açmadan art arda değiştirebilir.
        /// Bir alt menüdeki tıklama üst menüleri de kapatmaya çalıştığı için her menü seviyesine ayrı ayrı bağlanır.
        /// </summary>
        private void KeepOpenOnSettingClicks(ToolStripDropDown dropDown)
        {
            dropDown.ItemClicked += (_, e) => _lastClickKeepsMenuOpen = ReferenceEquals(e.ClickedItem?.Tag, KeepMenuOpenTag);
            dropDown.Closing += (_, e) =>
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked && _lastClickKeepsMenuOpen)
                {
                    e.Cancel = true;
                }
            };
        }

        private static void AddActionItem(ToolStrip menu, string text, Action onClick)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += (_, _) => onClick();
            menu.Items.Add(item);
        }

        private static void AddRadioOptions<T>(
            ToolStripMenuItem parent,
            IEnumerable<(string Label, T Value)> options,
            Func<T, bool> isSelected,
            Action<T> onSelected)
        {
            var groupItems = new List<ToolStripMenuItem>();
            foreach (var (label, value) in options)
            {
                var item = new ToolStripMenuItem(label)
                {
                    Checked = isSelected(value),
                    Tag = KeepMenuOpenTag
                };

                item.Click += (_, _) =>
                {
                    foreach (var other in groupItems) other.Checked = false;
                    item.Checked = true;
                    onSelected(value);
                };

                groupItems.Add(item);
                parent.DropDownItems.Add(item);
            }
        }

        public void UpdateStatus(int level, int satietyPercent, string stateDisplayName, int bugsCaught)
        {
            string nextText = $"🕷️ Sv.{level} | Tokluk: %{satietyPercent} | {stateDisplayName} | Av: {bugsCaught}";
            if (_lastStatusText != nextText)
            {
                _lastStatusText = nextText;
                _statusMenuItem.Text = nextText;
            }
        }

        public void Dispose()
        {
            Icon? icon = _notifyIcon.Icon;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            icon?.Dispose();
        }
    }
}
