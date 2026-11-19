using System;
using System.Windows.Forms;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Tepsi menüsünün küçük yardımcıları: eylem maddesi ve başlık.
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

        /// <summary>Tıklanamayan, gri bir başlık (örneğin örümceğin durum satırı).</summary>
        public static ToolStripMenuItem Header(string text) => new(text) { Enabled = false };

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
