using System;
using System.Windows;
using System.Windows.Interop;
using SpiderDesk.Core.Services;
using SpiderDesk.Domain.Performance;

namespace SpiderDesk.Infrastructure.Windowing
{
    /// <summary>
    /// Pencerenin tıklama geçirgenliğini (Click-Through) ve Masaüstü Z-Sırasını (Z-Order) yönetir.
    /// "Her zaman üstte" yalnızca "Pencerelerin Üstünde Göster" açıkken ya da bir duyuru yazılırken (IsPresentingOnTop)
    /// kullanılır; normal modda pencere doğrudan
    /// masaüstü duvar kağıdı/ikon katmanının (Progman/WorkerW) hemen üstünde tutulur. Böylece masaüstüne
    /// tıklandığında ekran yenilenmesi/titremesi olmaz ve pencereler alta alındığında masaüstüne tıklamaya gerek kalmaz.
    /// Pencereleri <see cref="IDesktopWindows"/> ile yerleştirir, pencere durumlarını <see cref="IWindowInspector"/> ile okur,
    /// örtülme kararını <see cref="DesktopOcclusionPolicy"/>'ye bırakır. Windows'u doğrudan çağırmadığı için testlerde
    /// sahte bir masaüstüyle çalıştırılabilir.
    /// </summary>
    public class WindowLayerService : IWindowLayerService
    {
        // Masaüstünün arkasına düşüp düşmediğimizi ararken en fazla bu kadar pencereye bakılır
        private const int MaxWindowsToSearchBelowDesktop = 32;

        private readonly IDesktopWindows _desktop;
        private readonly IWindowInspector _windowInspector;
        private IntPtr _hwnd;
        private IntPtr _desktopHwnd;
        private bool? _lastAppliedTopmostState;
        private bool _alwaysOnTop;
        private bool _isPresentingOnTop;
        private bool _isOverlayInteractive;
        private bool _isDesktopCovered;
        private int _tickCounter;

        public WindowLayerService(IDesktopWindows desktop, IWindowInspector windowInspector)
        {
            _desktop = desktop;
            _windowInspector = windowInspector;
        }

        /// <summary>
        /// Fare doğrudan görünür masaüstünün üzerindeyse ya da katman en öndeyse ("Pencerelerin Üstünde Göster" açıkken
        /// veya bir duyuru/not gösterilirken) true döner. Böylece tam ekran bir uygulamanın üstünde not gösteren örümcek
        /// de fareyle uzaklaştırılabilir. Pencereler simge durumuna küçültüldüğünde masaüstüne tekrar tıklamaya gerek kalmadan true olur.
        /// </summary>
        public bool IsOverlayInteractive => IsTopmost || _isOverlayInteractive;

        /// <summary>
        /// Öndeki ya da imlecin altındaki pencere, katmanın bulunduğu monitörde büyütülmüş veya
        /// monitörü tamamen kaplıyorsa (kenarlıksız tam ekran oyun/video) true döner.
        /// </summary>
        public bool IsDesktopOccluded => !IsTopmost && _isDesktopCovered;

        public bool IsDesktopCovered => !_alwaysOnTop && _isDesktopCovered;

        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                if (_alwaysOnTop != value)
                {
                    _alwaysOnTop = value;
                    _lastAppliedTopmostState = null;
                    EnforceZOrder();
                }
            }
        }

        public bool IsPresentingOnTop
        {
            get => _isPresentingOnTop;
            set
            {
                if (_isPresentingOnTop != value)
                {
                    _isPresentingOnTop = value;
                    _lastAppliedTopmostState = null;
                    EnforceZOrder();
                }
            }
        }

        private bool IsTopmost => _alwaysOnTop || _isPresentingOnTop;

        public void Attach(Window window) => Attach(new WindowInteropHelper(window).Handle);

        /// <summary>Pencere tanıtıcısıyla bağlanır; testler sahte masaüstündeki bir pencereyi bu yolla verir.</summary>
        public void Attach(IntPtr hwnd)
        {
            _hwnd = hwnd;
            _desktop.MakeClickThrough(_hwnd);

            _desktopHwnd = _desktop.FindDesktopHost();
            _lastAppliedTopmostState = null;
            EnforceZOrder();
        }

        public void EnforceZOrder()
        {
            if (_hwnd == IntPtr.Zero) return;

            if (IsTopmost)
            {
                if (_lastAppliedTopmostState != true)
                {
                    _desktop.SetTopmost(_hwnd, true);
                    _lastAppliedTopmostState = true;
                }

                if (_alwaysOnTop)
                {
                    _isOverlayInteractive = true;
                    _isDesktopCovered = false;
                    return;
                }

                // Duyuru için geçici olarak öndeyken masaüstünün örtülü olup olmadığını izlemeye devam et:
                // örtülüyse yazı süresince ağlar ve böcekler gizlenir
                UpdateVisibilityAndInteractionState();
                return;
            }

            if (_lastAppliedTopmostState != false)
            {
                _desktop.SetTopmost(_hwnd, false);
                PlaceDirectlyAboveDesktop();
                _lastAppliedTopmostState = false;
            }
            else
            {
                // Pencere sorgularını her karede değil 4 karede bir yaparak Windows'a yapılan çağrıları azalt
                _tickCounter++;
                if ((_tickCounter & 3) != 0)
                {
                    return;
                }

                if (_desktopHwnd != IntPtr.Zero && IsBehindDesktop())
                {
                    PlaceDirectlyAboveDesktop();
                }
            }

            UpdateVisibilityAndInteractionState();
        }

        private void PlaceDirectlyAboveDesktop()
        {
            // Explorer yeniden başlamışsa masaüstü penceresi değişmiştir; yenisini bul
            if (_desktopHwnd == IntPtr.Zero || !_windowInspector.IsDesktopWindow(_desktopHwnd))
            {
                _desktopHwnd = _desktop.FindDesktopHost();
            }

            if (_desktopHwnd == IntPtr.Zero) return;

            IntPtr windowAboveDesktop = _desktop.WindowAbove(_desktopHwnd);
            if (windowAboveDesktop != _hwnd)
            {
                bool aboveIsTopmost = windowAboveDesktop != IntPtr.Zero && _desktop.IsTopmost(windowAboveDesktop);

                // Masaüstünün hemen üstünde "her zaman üstte" bir pencere varsa ya da hiç pencere yoksa,
                // normal pencerelerin en üstüne çık; yoksa masaüstünün üstündeki pencerenin hemen altına gir
                if (windowAboveDesktop == IntPtr.Zero || aboveIsTopmost)
                {
                    _desktop.BringToTop(_hwnd);
                }
                else
                {
                    _desktop.PlaceBehind(_hwnd, windowAboveDesktop);
                }
            }
        }

        private bool IsBehindDesktop()
        {
            IntPtr current = _desktop.WindowBelow(_desktopHwnd);
            int guard = 0;
            while (current != IntPtr.Zero && guard++ < MaxWindowsToSearchBelowDesktop)
            {
                if (current == _hwnd)
                {
                    return true;
                }
                current = _desktop.WindowBelow(current);
            }
            return false;
        }

        private void UpdateVisibilityAndInteractionState()
        {
            bool cursorOverDesktop = false;
            IntPtr hitHwnd = _desktop.WindowUnderCursor(out IntPtr rootHwnd);
            if (hitHwnd != IntPtr.Zero)
            {
                cursorOverDesktop = (rootHwnd != IntPtr.Zero && rootHwnd == _desktopHwnd) ||
                                    _windowInspector.IsDesktopWindow(rootHwnd) ||
                                    _windowInspector.IsDesktopWindow(hitHwnd);
            }

            _isOverlayInteractive = cursorOverDesktop;

            if (cursorOverDesktop)
            {
                _isDesktopCovered = false;
                return;
            }

            _isDesktopCovered = OccludesOverlay(_desktop.ForegroundWindow()) || OccludesOverlay(rootHwnd);
        }

        private bool OccludesOverlay(IntPtr hwnd) =>
            hwnd != _hwnd && DesktopOcclusionPolicy.Occludes(_windowInspector.Capture(hwnd, _hwnd));
    }
}
