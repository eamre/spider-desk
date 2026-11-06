namespace SpiderDesk.Domain
{
    /// <summary>
    /// Bir pencerenin, karar anındaki görsel durumunun işletim sisteminden bağımsız özeti.
    /// Varsayılan değer (default) "pencere yok" anlamına gelir ve hiçbir zaman örtme sayılmaz.
    /// </summary>
    /// <param name="IsDesktop">Pencere masaüstü kabuğunun (Progman/WorkerW/ikon listesi) bir parçası mı?</param>
    /// <param name="IsMinimized">Pencere simge durumuna küçültülmüş mü?</param>
    /// <param name="IsMaximized">Pencere ekranı kaplayacak şekilde büyütülmüş (maximized) mü?</param>
    /// <param name="CoversMonitor">Pencere bulunduğu monitörün tamamını kaplıyor mu? (kenarlıksız tam ekran oyun/video)</param>
    /// <param name="IsOnOverlayMonitor">Pencere, SpiderDesk katmanının çizildiği monitörde mi?</param>
    public readonly record struct WindowSnapshot(
        bool IsDesktop,
        bool IsMinimized,
        bool IsMaximized,
        bool CoversMonitor,
        bool IsOnOverlayMonitor);

    /// <summary>
    /// Bir pencerenin SpiderDesk katmanını (dolayısıyla masaüstünü) kullanıcının gözünden gizleyip gizlemediğine
    /// karar veren saf kural sınıfı (Pure Policy). WPF/Win32 bağımlılığı yoktur; birim testle doğrulanabilir.
    /// </summary>
    public static class DesktopOcclusionPolicy
    {
        /// <summary>
        /// Pencere aynı monitörde, görünür durumda ve büyütülmüş ya da monitörü tamamen kaplıyorsa true döner.
        /// Masaüstünün kendisi de monitörü kapladığı için açıkça hariç tutulur.
        /// </summary>
        public static bool Occludes(WindowSnapshot window) =>
            !window.IsDesktop
            && !window.IsMinimized
            && window.IsOnOverlayMonitor
            && (window.IsMaximized || window.CoversMonitor);
    }
}
