using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Sistem tepsisindeki örümcek ikonu: ipucu yazısı, Windows bildirimleri ve küçük bir sağ tık menüsü
    /// (Ayarlar, Duraklat, Çıkış). İkona sol tıklamak ayarlar penceresini açıp kapatır; örümceğin durumu, komutları,
    /// Pomodoro, hatırlatıcılar ve ayarlar oradaki sekmelerdedir. Pomodoro'nun geri sayımı yine de ikonun ipucunda görünür.
    /// </summary>
    public sealed class TrayMenuService : INotificationService, ITrayTooltip, IDisposable
    {
        private const string DefaultTooltip = "SpiderDesk - Masaüstü Örümceği";

        private readonly NotifyIcon _notifyIcon;

        /// <summary>Menüden "Ayarlar" seçildi: pencere açılmalı.</summary>
        public event Action? SettingsWindowRequested;

        /// <summary>İkona sol tıklandı: pencere kapalıysa açılmalı, açıksa kapanmalı.</summary>
        public event Action? IconClicked;

        public event Action<bool>? PauseChanged;
        public event Action? ExitRequested;

        public TrayMenuService()
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add(MenuAction("⚙️ Ayarlar", () => SettingsWindowRequested?.Invoke()));
            menu.Items.Add(BuildPauseItem());

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MenuAction("❌ Çıkış", () => ExitRequested?.Invoke()));

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

        /// <summary>Tıklanınca bir iş yapan ve menüyü kapatan madde.</summary>
        private static ToolStripMenuItem MenuAction(string text, Action onClick)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += (_, _) => onClick();
            return item;
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
