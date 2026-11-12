using System;
using SpiderDesk.Domain;

namespace SpiderDesk.Infrastructure.Windowing
{
    /// <summary>
    /// İşletim sistemi pencerelerini sorgulayan altyapı soyutlaması.
    /// <see cref="WindowLayerService"/> Win32 ayrıntılarını bilmeden pencere durumlarını bu arayüz üzerinden okur.
    /// </summary>
    public interface IWindowInspector
    {
        /// <summary>Pencere masaüstü kabuğuna (Progman/WorkerW/ikon listesi) aitse true döner.</summary>
        bool IsDesktopWindow(IntPtr hwnd);

        /// <summary>
        /// Pencerenin anlık durumunu, SpiderDesk katmanının bulunduğu monitöre göre özetler.
        /// <paramref name="hwnd"/> boşsa varsayılan (örtmeyen) bir anlık görüntü döner.
        /// </summary>
        WindowSnapshot Capture(IntPtr hwnd, IntPtr overlayHwnd);
    }
}
