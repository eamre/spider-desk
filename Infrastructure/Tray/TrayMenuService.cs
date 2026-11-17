using System;
using System.Drawing;
using System.Windows.Forms;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Sistem tepsisindeki örümcek ikonu: ipucu yazısı, Windows bildirimleri ve sağ tık menüsü.
    /// Menünün kendisi parçalardan kurulur; her parça kendi sınıfındadır ve ilgili denetleyici yalnızca kendi parçasını tanır:
    /// <list type="bullet">
    /// <item><see cref="PomodoroMenu"/>: Pomodoro (PomodoroSessionController kullanır)</item>
    /// <item><see cref="ReminderMenu"/>: hatırlatıcılar (ReminderSessionController kullanır)</item>
    /// <item><see cref="SettingsMenu"/>: kayıtlı ayarlar (OverlaySessionController kullanır)</item>
    /// </list>
    /// Bu sınıfta kalanlar: örümceğin durum satırı, anlık eylemler (yeni ağ, yağmur, böcek...), Duraklat ve Çıkış.
    /// </summary>
    public sealed class TrayMenuService : INotificationService, IDisposable
    {
        private const string DefaultTooltip = "SpiderDesk - Masaüstü Örümceği";

        private readonly NotifyIcon _notifyIcon;
        private readonly ToolStripMenuItem _statusMenuItem;

        public PomodoroMenu Pomodoro { get; }
        public ReminderMenu Reminders { get; }
        public SettingsMenu Settings { get; }

        public event Action? NewWebRequested;
        public event Action? RappelRequested;
        public event Action? RainOnWebsRequested;
        public event Action<int>? SpawnBugsRequested;
        public event Action? ClearWebsRequested;
        public event Action<bool>? PauseChanged;
        public event Action? ExitRequested;

        public TrayMenuService(UserSettings currentSettings, bool initialStartupEnabled)
        {
            var keepOpen = new KeepMenuOpen();
            Pomodoro = new PomodoroMenu(currentSettings.PomodoroPreset, keepOpen);
            Reminders = new ReminderMenu(keepOpen);
            Settings = new SettingsMenu(currentSettings, initialStartupEnabled, keepOpen);

            var menu = new ContextMenuStrip();

            _statusMenuItem = TrayMenuItems.Header("🕷️ Seviye 1 | Başlatılıyor...");
            menu.Items.Add(_statusMenuItem);
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(TrayMenuItems.Action("🕸️ Hemen Yeni Ağ Ör", () => NewWebRequested?.Invoke()));
            menu.Items.Add(TrayMenuItems.Action("🧗 Tavandan İple Sark", () => RappelRequested?.Invoke()));
            menu.Items.Add(TrayMenuItems.Action("💧 Ağlara Çiy/Su Damlası Bırak", () => RainOnWebsRequested?.Invoke()));
            menu.Items.Add(TrayMenuItems.Action("🐛 Ekrana Böcek Gönder (3 Adet)", () => SpawnBugsRequested?.Invoke(3)));
            menu.Items.Add(TrayMenuItems.Action("🧹 Ağları Temizle", () => ClearWebsRequested?.Invoke()));

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Pomodoro.Root);
            menu.Items.Add(Reminders.Root);

            menu.Items.Add(new ToolStripSeparator());
            foreach (ToolStripItem item in Settings.Items)
            {
                menu.Items.Add(item);
            }
            menu.Items.Add(BuildPauseItem());

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(TrayMenuItems.Action("❌ Çıkış", () => ExitRequested?.Invoke()));

            keepOpen.AttachTo(menu);

            _notifyIcon = new NotifyIcon
            {
                Icon = TrayIconFactory.CreateSpiderIcon(),
                Text = DefaultTooltip,
                ContextMenuStrip = menu,
                Visible = true
            };

            _notifyIcon.DoubleClick += (_, _) => SpawnBugsRequested?.Invoke(2);

            // Pomodoro çalışırken geri sayım, menüyü açmadan da görülsün diye ikonun ipucunda da yazar
            Pomodoro.CountdownChanged += countdown => SetTooltip(countdown != null ? $"SpiderDesk · 🍅 {countdown}" : DefaultTooltip);
        }

        private ToolStripMenuItem BuildPauseItem()
        {
            var pauseItem = new ToolStripMenuItem("⏸️ Duraklat") { CheckOnClick = true };
            pauseItem.CheckedChanged += (_, _) =>
            {
                bool paused = pauseItem.Checked;
                pauseItem.Text = paused ? "▶️ Devam Et" : "⏸️ Duraklat";
                PauseChanged?.Invoke(paused);
            };
            return pauseItem;
        }

        public void UpdateStatus(int level, int satietyPercent, string stateDisplayName, int bugsCaught) =>
            TrayMenuItems.SetTextIfChanged(_statusMenuItem, $"🕷️ Sv.{level} | Tokluk: %{satietyPercent} | {stateDisplayName} | Av: {bugsCaught}");

        /// <summary>Ekranın sağ altında Windows bildirimi gösterir; tam ekran bir uygulama açıkken de görünür.</summary>
        public void ShowNotification(string title, string message) =>
            _notifyIcon.ShowBalloonTip(6000, title, message, ToolTipIcon.Info);

        private void SetTooltip(string text)
        {
            if (_notifyIcon.Text != text)
            {
                _notifyIcon.Text = text;
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
