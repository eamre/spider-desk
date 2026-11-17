using System;
using System.Windows;
using System.Windows.Interop;
using SpiderDesk.Core.Services;
using SpiderDesk.Domain.Performance;
using static SpiderDesk.Infrastructure.Windowing.NativeMethods;

namespace SpiderDesk.Infrastructure.Windowing
{
    /// <summary>
    /// Pencerenin tıklama geçirgenliğini (Click-Through) ve Masaüstü Z-Sırasını (Z-Order) yönetir.
    /// HWND_TOPMOST yalnızca "Pencerelerin Üstünde Göster" açıkken ya da bir duyuru yazılırken (IsPresentingOnTop)
    /// kullanılır; normal modda pencere doğrudan
    /// masaüstü duvar kağıdı/ikon katmanının (Progman/WorkerW) hemen üstünde tutulur. Böylece masaüstüne
    /// tıklandığında ekran yenilenmesi/titremesi olmaz ve pencereler alta alındığında masaüstüne tıklamaya gerek kalmaz.
    /// Pencere durumlarını <see cref="IWindowInspector"/> ile okur, örtülme kararını <see cref="DesktopOcclusionPolicy"/>'ye bırakır.
    /// Kullandığı Windows fonksiyonları ve sabitleri <see cref="NativeMethods"/>'tadır.
    /// </summary>
    public class WindowLayerService : IWindowLayerService
    {
        private readonly IWindowInspector _windowInspector;
        private IntPtr _hwnd;
        private IntPtr _desktopHwnd;
        private bool? _lastAppliedTopmostState;
        private bool _alwaysOnTop;
        private bool _isPresentingOnTop;
        private bool _isOverlayInteractive;
        private bool _isDesktopCovered;
        private int _tickCounter;

        public WindowLayerService(IWindowInspector windowInspector)
        {
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

        public void Attach(Window window)
        {
            _hwnd = new WindowInteropHelper(window).Handle;
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

            _desktopHwnd = FindDesktopHostWindow();
            _lastAppliedTopmostState = null;
            EnforceZOrder();
        }

        public void EnforceZOrder()
        {
            if (_hwnd == IntPtr.Zero) return;

            // SWP_NOREDRAW ve SWP_DEFERERASE: Z-sırası değişirken Windows'un alttaki masaüstü ikon listesini
            // (SysListView32) silip baştan boyamasını (F5 yenilenme hissini) tamamen engeller.
            const uint flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOSENDCHANGING | SWP_NOREDRAW | SWP_DEFERERASE;

            if (IsTopmost)
            {
                if (_lastAppliedTopmostState != true)
                {
                    SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, flags);
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
                SetWindowPos(_hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, flags);
                PlaceDirectlyAboveDesktop(flags);
                _lastAppliedTopmostState = false;
            }
            else
            {
                // Win32 pencere sorgularını her kare yerine 3 karede bir (~50 ms) çalıştırarak P/Invoke yükünü azalt
                _tickCounter++;
                if ((_tickCounter & 3) != 0)
                {
                    return;
                }

                if (_desktopHwnd != IntPtr.Zero && IsBehindDesktop(_hwnd, _desktopHwnd))
                {
                    PlaceDirectlyAboveDesktop(flags);
                }
            }

            UpdateVisibilityAndInteractionState();
        }

        private void PlaceDirectlyAboveDesktop(uint flags)
        {
            if (_desktopHwnd == IntPtr.Zero || !_windowInspector.IsDesktopWindow(_desktopHwnd))
            {
                _desktopHwnd = FindDesktopHostWindow();
            }

            if (_desktopHwnd == IntPtr.Zero) return;

            IntPtr windowAboveDesktop = GetWindow(_desktopHwnd, GW_HWNDPREV);
            if (windowAboveDesktop != _hwnd)
            {
                bool aboveIsTopmost = windowAboveDesktop != IntPtr.Zero &&
                                      (GetWindowLong(windowAboveDesktop, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0;

                IntPtr insertAfter = (windowAboveDesktop == IntPtr.Zero || aboveIsTopmost)
                    ? HWND_TOP
                    : windowAboveDesktop;

                SetWindowPos(_hwnd, insertAfter, 0, 0, 0, 0, flags);
            }
        }

        private static bool IsBehindDesktop(IntPtr targetHwnd, IntPtr desktopHwnd)
        {
            IntPtr current = GetWindow(desktopHwnd, GW_HWNDNEXT);
            int guard = 0;
            while (current != IntPtr.Zero && guard++ < 32)
            {
                if (current == targetHwnd)
                {
                    return true;
                }
                current = GetWindow(current, GW_HWNDNEXT);
            }
            return false;
        }

        private void UpdateVisibilityAndInteractionState()
        {
            bool cursorOverDesktop = false;
            IntPtr rootHwnd = IntPtr.Zero;
            if (GetCursorPos(out POINT pt))
            {
                IntPtr hitHwnd = WindowFromPoint(pt);
                if (hitHwnd != IntPtr.Zero)
                {
                    rootHwnd = GetAncestor(hitHwnd, GA_ROOT);
                    cursorOverDesktop = (rootHwnd != IntPtr.Zero && rootHwnd == _desktopHwnd) ||
                                        _windowInspector.IsDesktopWindow(rootHwnd) ||
                                        _windowInspector.IsDesktopWindow(hitHwnd);
                }
            }

            _isOverlayInteractive = cursorOverDesktop;

            if (cursorOverDesktop)
            {
                _isDesktopCovered = false;
                return;
            }

            _isDesktopCovered = OccludesOverlay(GetForegroundWindow()) || OccludesOverlay(rootHwnd);
        }

        private bool OccludesOverlay(IntPtr hwnd) =>
            hwnd != _hwnd && DesktopOcclusionPolicy.Occludes(_windowInspector.Capture(hwnd, _hwnd));

        private static IntPtr FindDesktopHostWindow()
        {
            IntPtr progman = FindWindow("Progman", null);
            if (progman != IntPtr.Zero && FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
            {
                return progman;
            }

            IntPtr workerW = IntPtr.Zero;
            while ((workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null)) != IntPtr.Zero)
            {
                if (FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                {
                    return workerW;
                }
            }

            return progman;
        }
    }
}
