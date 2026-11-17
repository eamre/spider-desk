using System.Windows.Forms;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Windows menüleri her tıklamada kapanır. Bu sınıf, işaretlenmiş öğelere (ayar seçimleri, Başlat/Durdur,
    /// hatırlatma silme) tıklanınca menünün kapanmasını engeller; böylece kullanıcı birkaç ayarı menüyü yeniden
    /// açmadan art arda değiştirebilir ve değişikliği hemen görür. Eylemler (örneğin "Yeni Ağ Ör") menüyü yine kapatır.
    /// </summary>
    public sealed class KeepMenuOpen
    {
        // Bu etiketi taşıyan öğelere tıklanınca menü açık kalır
        private readonly object _tag = new();
        private bool _lastClickKeepsMenuOpen;

        /// <summary>Bu öğeye tıklanınca menü açık kalsın.</summary>
        public T Mark<T>(T item) where T : ToolStripItem
        {
            item.Tag = _tag;
            return item;
        }

        /// <summary>
        /// Bir menü seviyesini bu kurala bağlar. Bir alt menüdeki tıklama üst menüleri de kapatmaya çalıştığı için
        /// her menü seviyesi (ana menü ve her alt menü) ayrı ayrı bağlanmalıdır.
        /// </summary>
        public void AttachTo(ToolStripDropDown dropDown)
        {
            dropDown.ItemClicked += (_, e) => _lastClickKeepsMenuOpen = ReferenceEquals(e.ClickedItem?.Tag, _tag);
            dropDown.Closing += (_, e) =>
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked && _lastClickKeepsMenuOpen)
                {
                    e.Cancel = true;
                }
            };
        }
    }
}
