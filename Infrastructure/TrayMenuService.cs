using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SpiderDesk.Core;
using SpiderDesk.Domain.Spider.Themes;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Sistem tepsisi (System Tray) ikonunu, tema seçicisini ve ayar menülerini yönetir (SRP).
    /// </summary>
    public sealed class TrayMenuService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ToolStripMenuItem _statusMenuItem;
        private string _lastStatusText = string.Empty;

        public event Action? NewWebRequested;
        public event Action? RappelRequested;
        public event Action? RainOnWebsRequested;
        public event Action<int>? SpawnBugsRequested;
        public event Action? ClearWebsRequested;
        public event Action<ISpiderTheme>? ThemeSelected;
        public event Action<BugSpawnRatePreset>? SpawnRateSelected;
        public event Action<double>? SpeedMultiplierSelected;
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

            var buildWebItem = new ToolStripMenuItem("🕸️ Hemen Yeni Ağ Ör");
            buildWebItem.Click += (_, _) => NewWebRequested?.Invoke();
            menu.Items.Add(buildWebItem);

            var rappelItem = new ToolStripMenuItem("🧗 Tavandan İple Sark");
            rappelItem.Click += (_, _) => RappelRequested?.Invoke();
            menu.Items.Add(rappelItem);

            var rainItem = new ToolStripMenuItem("💧 Ağlara Çiy/Su Damlası Bırak");
            rainItem.Click += (_, _) => RainOnWebsRequested?.Invoke();
            menu.Items.Add(rainItem);

            var spawnBugsItem = new ToolStripMenuItem("🪰 Ekrana Böcek Gönder (3 Adet)");
            spawnBugsItem.Click += (_, _) => SpawnBugsRequested?.Invoke(3);
            menu.Items.Add(spawnBugsItem);

            var clearWebsItem = new ToolStripMenuItem("🧹 Ağları Temizle");
            clearWebsItem.Click += (_, _) => ClearWebsRequested?.Invoke();
            menu.Items.Add(clearWebsItem);

            menu.Items.Add(new ToolStripSeparator());

            // 1. Örümcek Teması Alt Menüsü
            menu.Items.Add(BuildThemeSubMenu(currentSettings.ThemeId));

            // 2. Hız ve Böcek Sıklığı Alt Menüsü
            menu.Items.Add(BuildSimulationSettingsSubMenu(currentSettings));

            menu.Items.Add(new ToolStripSeparator());

            var alwaysOnTopItem = new ToolStripMenuItem("🖥️ Pencerelerin Üstünde Göster")
            {
                Checked = currentSettings.AlwaysOnTop,
                CheckOnClick = true
            };
            alwaysOnTopItem.CheckedChanged += (_, _) => AlwaysOnTopChanged?.Invoke(alwaysOnTopItem.Checked);
            menu.Items.Add(alwaysOnTopItem);

            var startupItem = new ToolStripMenuItem("🚀 Bilgisayar Açıldığında Otomatik Başlat")
            {
                Checked = initialStartupEnabled,
                CheckOnClick = true
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

            var exitItem = new ToolStripMenuItem("❌ Çıkış");
            exitItem.Click += (_, _) => ExitRequested?.Invoke();
            menu.Items.Add(exitItem);

            _notifyIcon = new NotifyIcon
            {
                Icon = CreateSpiderTrayIcon(),
                Text = "SpiderDesk - Masaüstü Örümceği",
                ContextMenuStrip = menu,
                Visible = true
            };

            _notifyIcon.DoubleClick += (_, _) => SpawnBugsRequested?.Invoke(2);
        }

        private ToolStripMenuItem BuildThemeSubMenu(string currentThemeId)
        {
            var themeRoot = new ToolStripMenuItem("🎨 Örümcek Teması");
            var themeItems = new List<ToolStripMenuItem>();

            foreach (var theme in SpiderThemeCatalog.All)
            {
                var item = new ToolStripMenuItem(theme.DisplayName)
                {
                    Checked = theme.Id == currentThemeId
                };

                item.Click += (_, _) =>
                {
                    foreach (var other in themeItems) other.Checked = false;
                    item.Checked = true;
                    ThemeSelected?.Invoke(theme);
                };

                themeItems.Add(item);
                themeRoot.DropDownItems.Add(item);
            }

            return themeRoot;
        }

        private ToolStripMenuItem BuildSimulationSettingsSubMenu(UserSettings settings)
        {
            var settingsRoot = new ToolStripMenuItem("⚙️ Hız ve Böcek Sıklığı");

            // Böcek Sıklığı Seçenekleri
            var rateHeader = new ToolStripMenuItem("🪰 Böcek Çıkma Sıklığı") { Enabled = false };
            settingsRoot.DropDownItems.Add(rateHeader);

            var rateOptions = new (string Label, BugSpawnRatePreset Preset)[]
            {
                ("Seyrek (45-75 sn)", BugSpawnRatePreset.Low),
                ("Normal (20-38 sn)", BugSpawnRatePreset.Normal),
                ("Yoğun (8-17 sn)", BugSpawnRatePreset.High)
            };

            var rateMenuItems = new List<ToolStripMenuItem>();
            foreach (var opt in rateOptions)
            {
                var item = new ToolStripMenuItem(opt.Label)
                {
                    Checked = settings.SpawnRate == opt.Preset
                };
                item.Click += (_, _) =>
                {
                    foreach (var other in rateMenuItems) other.Checked = false;
                    item.Checked = true;
                    SpawnRateSelected?.Invoke(opt.Preset);
                };
                rateMenuItems.Add(item);
                settingsRoot.DropDownItems.Add(item);
            }

            settingsRoot.DropDownItems.Add(new ToolStripSeparator());

            // Örümcek Hızı Seçenekleri
            var speedHeader = new ToolStripMenuItem("⚡ Örümcek Hızı") { Enabled = false };
            settingsRoot.DropDownItems.Add(speedHeader);

            var speedOptions = new (string Label, double Multiplier)[]
            {
                ("Sakin (0.75x)", 0.75),
                ("Normal (1.0x)", 1.0),
                ("Çevik (1.35x)", 1.35)
            };

            var speedMenuItems = new List<ToolStripMenuItem>();
            foreach (var opt in speedOptions)
            {
                var item = new ToolStripMenuItem(opt.Label)
                {
                    Checked = Math.Abs(settings.SpeedMultiplier - opt.Multiplier) < 0.05
                };
                item.Click += (_, _) =>
                {
                    foreach (var other in speedMenuItems) other.Checked = false;
                    item.Checked = true;
                    SpeedMultiplierSelected?.Invoke(opt.Multiplier);
                };
                speedMenuItems.Add(item);
                settingsRoot.DropDownItems.Add(item);
            }

            settingsRoot.DropDownItems.Add(new ToolStripSeparator());

            // Gece/Gündüz Zaman Modu Seçenekleri
            var timeHeader = new ToolStripMenuItem("🌙 Gece/Gündüz Döngüsü") { Enabled = false };
            settingsRoot.DropDownItems.Add(timeHeader);

            var timeOptions = new (string Label, EnvironmentTimeMode Mode)[]
            {
                ("Otomatik (Sistem Saati)", EnvironmentTimeMode.AutoBySystemClock),
                ("Sürekli Gündüz", EnvironmentTimeMode.AlwaysDay),
                ("Sürekli Gece (Ateş Böcekleri)", EnvironmentTimeMode.AlwaysNight)
            };

            var timeMenuItems = new List<ToolStripMenuItem>();
            foreach (var opt in timeOptions)
            {
                var item = new ToolStripMenuItem(opt.Label)
                {
                    Checked = settings.TimeMode == opt.Mode
                };
                item.Click += (_, _) =>
                {
                    foreach (var other in timeMenuItems) other.Checked = false;
                    item.Checked = true;
                    TimeModeSelected?.Invoke(opt.Mode);
                };
                timeMenuItems.Add(item);
                settingsRoot.DropDownItems.Add(item);
            }

            return settingsRoot;
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
            _notifyIcon.Dispose();
        }

        private static Icon CreateSpiderTrayIcon()
        {
            using Bitmap bmp = new Bitmap(32, 32);
            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var legPen = new Pen(Color.FromArgb(240, 30, 30, 35), 2.2f);
            g.DrawLines(legPen, new[] { new Point(16, 15), new Point(6, 7), new Point(2, 3) });
            g.DrawLines(legPen, new[] { new Point(16, 15), new Point(26, 7), new Point(30, 3) });
            g.DrawLines(legPen, new[] { new Point(16, 16), new Point(4, 13), new Point(1, 12) });
            g.DrawLines(legPen, new[] { new Point(16, 16), new Point(28, 13), new Point(31, 12) });
            g.DrawLines(legPen, new[] { new Point(16, 17), new Point(5, 20), new Point(2, 23) });
            g.DrawLines(legPen, new[] { new Point(16, 17), new Point(27, 20), new Point(30, 23) });
            g.DrawLines(legPen, new[] { new Point(16, 18), new Point(7, 25), new Point(4, 30) });
            g.DrawLines(legPen, new[] { new Point(16, 18), new Point(25, 25), new Point(28, 30) });

            g.FillEllipse(Brushes.Black, 10, 16, 12, 13);
            g.FillEllipse(Brushes.Black, 12, 10, 8, 8);
            g.FillEllipse(Brushes.Crimson, 14, 20, 4, 5);
            g.FillEllipse(Brushes.Red, 13, 11, 2, 2);
            g.FillEllipse(Brushes.Red, 17, 11, 2, 2);

            return Icon.FromHandle(bmp.GetHicon());
        }
    }
}
