using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Tepsideki "⏰ Hatırlatıcılar" alt menüsü: en üstte "ekle", altında bekleyen hatırlatmalar.
    /// Bir hatırlatmaya tıklamak onu siler; menü açık kalır ki kullanıcı listenin güncellendiğini görsün.
    /// </summary>
    public sealed class ReminderMenu
    {
        private readonly KeepMenuOpen _keepOpen;

        /// <summary>Menüye eklenecek "⏰ Hatırlatıcılar" maddesi. Liste <see cref="ShowReminders"/> ile doldurulur.</summary>
        public ToolStripMenuItem Root { get; } = new("⏰ Hatırlatıcılar");

        public event Action? AddRequested;
        public event Action<Guid>? DeleteRequested;

        public ReminderMenu(KeepMenuOpen keepOpen)
        {
            _keepOpen = keepOpen;
            keepOpen.AttachTo(Root.DropDown);
        }

        public void ShowReminders(string title, IReadOnlyList<(Guid Id, string Label)> reminders)
        {
            TrayMenuItems.SetTextIfChanged(Root, title);

            var items = Root.DropDownItems;
            items.Clear();

            items.Add(TrayMenuItems.Action("➕ Hatırlatıcı Ekle…", () => AddRequested?.Invoke()));
            items.Add(new ToolStripSeparator());

            if (reminders.Count == 0)
            {
                items.Add(TrayMenuItems.Header("Bekleyen hatırlatma yok"));
                return;
            }

            items.Add(TrayMenuItems.Header("🗑️ Silmek için tıkla"));
            foreach (var (id, label) in reminders)
            {
                var item = _keepOpen.Mark(new ToolStripMenuItem(label));
                // Liste, tıklama olayı bitince yeniden kurulur; tıklanan öğe olay sürerken menüden kaldırılmamalı
                item.Click += (_, _) => RunAfterCurrentClick(() => DeleteRequested?.Invoke(id));
                items.Add(item);
            }
        }

        private static void RunAfterCurrentClick(Action action)
        {
            if (SynchronizationContext.Current is { } context)
            {
                context.Post(_ => action(), null);
            }
            else
            {
                action();
            }
        }
    }
}
