using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Tepsi menülerinin hepsinde kullanılan küçük yardımcılar: eylem maddesi, radyo seçenekleri, başlık.
    /// </summary>
    public static class TrayMenuItems
    {
        /// <summary>Tıklanınca bir iş yapan ve menüyü kapatan madde (örneğin "Yeni Ağ Ör").</summary>
        public static ToolStripMenuItem Action(string text, Action onClick)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += (_, _) => onClick();
            return item;
        }

        /// <summary>Tıklanamayan, gri bir başlık (örneğin "⏱️ Süre").</summary>
        public static ToolStripMenuItem Header(string text) => new(text) { Enabled = false };

        /// <summary>
        /// Aynı gruptan yalnızca birinin seçili olabildiği seçenekleri ekler (örneğin örümcek hızı).
        /// Seçeneklere tıklamak menüyü kapatmaz.
        /// </summary>
        public static void AddRadioOptions<T>(
            ToolStripMenuItem parent,
            IEnumerable<(string Label, T Value)> options,
            Func<T, bool> isSelected,
            Action<T> onSelected,
            KeepMenuOpen keepOpen)
        {
            var groupItems = new List<ToolStripMenuItem>();
            foreach (var (label, value) in options)
            {
                var item = keepOpen.Mark(new ToolStripMenuItem(label) { Checked = isSelected(value) });

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

        /// <summary>Yazı gerçekten değiştiyse günceller; menü her karede gereksiz yere yeniden çizilmez.</summary>
        public static void SetTextIfChanged(ToolStripItem item, string text)
        {
            if (item.Text != text)
            {
                item.Text = text;
            }
        }
    }
}
