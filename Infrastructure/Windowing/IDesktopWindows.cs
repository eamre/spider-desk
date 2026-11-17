using System;

namespace SpiderDesk.Infrastructure.Windowing
{
    /// <summary>
    /// Masaüstündeki pencerelerin üst üste sırasını (Z-sırası) okuyan ve değiştiren işlemler.
    /// <see cref="WindowLayerService"/> Windows'u doğrudan çağırmaz, bu arayüzü kullanır. Uygulamada arkasında gerçek Windows
    /// (<see cref="Win32DesktopWindows"/>), testlerde ise pencereleri basit bir listede tutan sahte bir masaüstü vardır.
    /// Pencereler tanıtıcılarıyla (hwnd) belirtilir; <see cref="IntPtr.Zero"/> "pencere yok" demektir.
    /// </summary>
    public interface IDesktopWindows
    {
        /// <summary>Tıklamalar pencerenin içinden geçsin; pencere görev çubuğunda görünmesin ve odak çalmasın.</summary>
        void MakeClickThrough(IntPtr hwnd);

        /// <summary>Duvar kâğıdını ve masaüstü ikonlarını taşıyan pencereyi (Progman ya da WorkerW) bulur.</summary>
        IntPtr FindDesktopHost();

        /// <summary>Sırada bu pencerenin hemen üstündeki pencere.</summary>
        IntPtr WindowAbove(IntPtr hwnd);

        /// <summary>Sırada bu pencerenin hemen altındaki pencere.</summary>
        IntPtr WindowBelow(IntPtr hwnd);

        /// <summary>Pencere "her zaman üstte" grubundaysa true döner.</summary>
        bool IsTopmost(IntPtr hwnd);

        /// <summary>Pencereyi "her zaman üstte" grubuna alır ya da o gruptan çıkarır.</summary>
        void SetTopmost(IntPtr hwnd, bool topmost);

        /// <summary>Pencereyi <paramref name="windowAbove"/>'un hemen altına yerleştirir.</summary>
        void PlaceBehind(IntPtr hwnd, IntPtr windowAbove);

        /// <summary>Pencereyi kendi grubunun en üstüne çıkarır.</summary>
        void BringToTop(IntPtr hwnd);

        /// <summary>Kullanıcının o an çalıştığı (öndeki) pencere.</summary>
        IntPtr ForegroundWindow();

        /// <summary>
        /// Fare imlecinin altındaki pencereyi verir; <paramref name="rootWindow"/> o pencerenin ait olduğu ana penceredir.
        /// İmlecin altında pencere yoksa ikisi de <see cref="IntPtr.Zero"/> olur.
        /// </summary>
        IntPtr WindowUnderCursor(out IntPtr rootWindow);
    }
}
