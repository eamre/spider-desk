using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using SpiderDesk.Core;
using SpiderDesk.Core.Services;
using SpiderDesk.Domain;

namespace SpiderDesk.Infrastructure.Windowing
{
    /// <summary>
    /// Pencerenin tıklama geçirgenliğini (Click-Through) ve Masaüstü Z-Sırasını (Z-Order) yönetir.
    /// HWND_TOPMOST yalnızca "Pencerelerin Üstünde Göster" açıkken kullanılır; normal modda pencere doğrudan
    /// masaüstü duvar kağıdı/ikon katmanının (Progman/WorkerW) hemen üstünde tutulur. Böylece masaüstüne
    /// tıklandığında ekran yenilenmesi/titremesi olmaz ve pencereler alta alındığında masaüstüne tıklamaya gerek kalmaz.
    /// Pencere durumlarını <see cref="IWindowInspector"/> ile okur, örtülme kararını <see cref="DesktopOcclusionPolicy"/>'ye bırakır.
    /// </summary>
    public class WindowLayerService : IWindowLayerService
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const uint GW_HWNDPREV = 3;
        private const uint GW_HWNDNEXT = 2;
        private const uint GA_ROOT = 2;

        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOREDRAW = 0x0008;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOSENDCHANGING = 0x0400;
        private const uint SWP_DEFERERASE = 0x2000;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        private readonly IWindowInspector _windowInspector;
        private IntPtr _hwnd;
        private IntPtr _desktopHwnd;
        private bool? _lastAppliedTopmostState;
        private bool _alwaysOnTop;
        private bool _isOverlayInteractive;
        private bool _isDesktopOccluded;
        private int _tickCounter;

        public WindowLayerService(IWindowInspector windowInspector)
        {
            _windowInspector = windowInspector;
        }

        /// <summary>
        /// Fare doğrudan görünür masaüstünün üzerindeyse veya "Pencerelerin Üstünde Göster" açıksa true döner.
        /// Pencereler simge durumuna küçültüldüğünde masaüstüne tekrar tıklamaya gerek kalmadan true olur.
        /// </summary>
        public bool IsOverlayInteractive => _alwaysOnTop || _isOverlayInteractive;

        /// <summary>
        /// Öndeki ya da imlecin altındaki pencere, katmanın bulunduğu monitörde büyütülmüş veya
        /// monitörü tamamen kaplıyorsa (kenarlıksız tam ekran oyun/video) true döner.
        /// </summary>
        public bool IsDesktopOccluded => !_alwaysOnTop && _isDesktopOccluded;

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

            if (_alwaysOnTop)
            {
                if (_lastAppliedTopmostState != true)
                {
                    SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, flags);
                    _lastAppliedTopmostState = true;
                }
                _isOverlayInteractive = true;
                _isDesktopOccluded = false;
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
                _isDesktopOccluded = false;
                return;
            }

            _isDesktopOccluded = OccludesOverlay(GetForegroundWindow()) || OccludesOverlay(rootHwnd);
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
