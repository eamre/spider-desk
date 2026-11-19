using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Sistem tepsisindeki örümcek ikonu: ipucu yazısı, Windows bildirimleri ve sağ tık menüsü.
    /// Menüde örümceğin durum satırı, ayarlar penceresini açma (sol tık ya da "Ayarlar"), anlık eylemler (yeni ağ, yağmur, böcek...),
    /// Duraklat ve Çıkış vardır. Ayarlar, Pomodoro ve hatırlatıcılar ayarlar penceresinin sekmelerindedir;
    /// Pomodoro'nun geri sayımı yine de ikonun ipucunda görünür.
    /// </summary>
    public sealed class TrayMenuService : INotificationService, ITrayTooltip, IDisposable
    {
        private const string DefaultTooltip = "SpiderDesk - Masaüstü Örümceği";

        private readonly NotifyIcon _notifyIcon;
        private readonly ToolStripMenuItem _statusMenuItem;

        /// <summary>Menüden "Ayarlar" seçildi: pencere açılmalı.</summary>
        public event Action? SettingsWindowRequested;

        /// <summary>İkona sol tıklandı: pencere kapalıysa açılmalı, açıksa kapanmalı.</summary>
        public event Action? IconClicked;

        public event Action? NewWebRequested;
        public event Action? RappelRequested;
        public event Action? RainOnWebsRequested;
        public event Action<int>? SpawnBugsRequested;
        public event Action? ClearWebsRequested;
        public event Action<bool>? PauseChanged;
        public event Action? ExitRequested;

        public TrayMenuService()
        {
            var menu = new ContextMenuStrip();

            _statusMenuItem = TrayMenuItems.Header("🕷️ Seviye 1 | Başlatılıyor...");
            menu.Items.Add(_statusMenuItem);
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(TrayMenuItems.Action("⚙️ Ayarlar", () => SettingsWindowRequested?.Invoke()));
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(TrayMenuItems.Action("🕸️ Hemen Yeni Ağ Ör", () => NewWebRequested?.Invoke()));
            menu.Items.Add(TrayMenuItems.Action("🧗 Tavandan İple Sark", () => RappelRequested?.Invoke()));
            menu.Items.Add(TrayMenuItems.Action("💧 Ağlara Çiy/Su Damlası Bırak", () => RainOnWebsRequested?.Invoke()));
            menu.Items.Add(TrayMenuItems.Action("🐛 Ekrana Böcek Gönder (3 Adet)", () => SpawnBugsRequested?.Invoke(3)));
            menu.Items.Add(TrayMenuItems.Action("🧹 Ağları Temizle", () => ClearWebsRequested?.Invoke()));

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(BuildPauseItem());

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(TrayMenuItems.Action("❌ Çıkış", () => ExitRequested?.Invoke()));

            _notifyIcon = new NotifyIcon
            {
                Icon = TrayIconFactory.CreateSpiderIcon(),
                Text = DefaultTooltip,
                ContextMenuStrip = menu,
                Visible = true
            };

            // Sol tık ayarlar penceresini açıp kapatır; sağ tık menüyü açar (bunu NotifyIcon kendisi yapar)
            _notifyIcon.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left) IconClicked?.Invoke();
            };
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

        /// <summary>Pomodoro çalışırken geri sayım, pencereyi açmadan da görülsün diye ikonun ipucunda yazar.</summary>
        public void ShowPomodoroCountdown(string? countdown) =>
            SetTooltip(countdown != null ? $"SpiderDesk · 🍅 {countdown}" : DefaultTooltip);

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
