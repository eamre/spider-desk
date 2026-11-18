using System;
using System.Windows;

namespace SpiderDesk.Infrastructure.Dialogs
{
    /// <summary>
    /// Tepsiden açılan pencerenin (flyout) ekrandaki yerini hesaplayan saf kural sınıfı.
    /// Pencere, tepsinin bulunduğu köşede görev çubuğunun hemen yanında açılır. Görev çubuğunun hangi kenarda olduğu,
    /// ekranın tamamı ile pencerelerin kullanabildiği alan (çalışma alanı) karşılaştırılarak bulunur.
    /// </summary>
    public static class FlyoutPlacement
    {
        /// <summary>Pencere ile ekran kenarı ve görev çubuğu arasındaki boşluk.</summary>
        public const double Gap = 12;

        /// <param name="screen">Ekranın tamamı.</param>
        /// <param name="workArea">Görev çubuğu dışında kalan alan.</param>
        /// <param name="windowSize">Pencerenin istenen boyutu.</param>
        /// <returns>Pencerenin sol üst köşesi ve sığması için gerekirse kısaltılmış yüksekliği.</returns>
        public static (Point TopLeft, double Height) Place(Rect screen, Rect workArea, Size windowSize)
        {
            double height = Math.Min(windowSize.Height, Math.Max(0, workArea.Height - 2 * Gap));

            // Tepsi, görev çubuğunun sağ ya da alt ucundadır; görev çubuğu soldaysa pencere sola, üstteyse yukarı yaslanır
            bool taskbarOnLeft = workArea.Left > screen.Left;
            bool taskbarOnTop = workArea.Top > screen.Top;

            double left = taskbarOnLeft ? workArea.Left + Gap : workArea.Right - windowSize.Width - Gap;
            double top = taskbarOnTop ? workArea.Top + Gap : workArea.Bottom - height - Gap;

            return (new Point(left, top), height);
        }
    }
}
