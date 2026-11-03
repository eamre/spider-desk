using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using SpiderDesk.Core;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Pencerenin tıklama geçirgenliğini (Click-Through) ve Masaüstü Z-Sırasını (Z-Order) yönetir.
    /// HWND_TOPMOST yalnızca "Pencerelerin Üstünde Göster" açıkken kullanılır; normal modda pencere doğrudan
    /// masaüstü duvar kağıdı/ikon katmanının (Progman/WorkerW) hemen üstünde tutulur. Böylece masaüstüne
    /// tıklandığında ekran yenilenmesi/titremesi olmaz ve pencereler alta alındığında masaüstüne tıklamaya gerek kalmaz.
    /// </summary>
    public class WindowLayerService : IWindowLayerService
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const uint GW_HWNDPREV = 3;
        private const uint GA_ROOT = 2;

        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOSENDCHANGING = 0x0400;

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

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private IntPtr _hwnd;
        private IntPtr _desktopHwnd;
        private bool? _lastAppliedTopmostState;
        private bool _alwaysOnTop;
        private bool _isOverlayInteractive;
        private bool _isDesktopOccluded;

        /// <summary>
        /// Fare doğrudan görünür masaüstünün üzerindeyse veya "Pencerelerin Üstünde Göster" açıksa true döner.
        /// Pencereler simge durumuna küçültüldüğünde masaüstüne tekrar tıklamaya gerek kalmadan true olur.
        /// </summary>
        public bool IsOverlayInteractive => _alwaysOnTop || _isOverlayInteractive;

        /// <summary>
        /// Öndeki pencere tam ekran/büyütülmüş (maximized) ise ve masaüstünü tamamen kapatıyorsa true döner.
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

            const uint flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOSENDCHANGING;

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
                _lastAppliedTopmostState = false;
            }

            if (_desktopHwnd == IntPtr.Zero || !IsDesktopWindow(_desktopHwnd))
            {
                _desktopHwnd = FindDesktopHostWindow();
            }

            // Pencereyi her zaman masaüstü ikon katmanının (_desktopHwnd) hemen üstünde,
            // normal pencerelerin ise altında tut (Topmost bayrağını hiç değiştirmeden, sıfır ekran titremesiyle)
            if (_desktopHwnd != IntPtr.Zero)
            {
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

            UpdateVisibilityAndInteractionState();
        }

        private void UpdateVisibilityAndInteractionState()
        {
            bool cursorOverDesktop = false;
            if (GetCursorPos(out POINT pt))
            {
                IntPtr hitHwnd = WindowFromPoint(pt);
                if (hitHwnd != IntPtr.Zero)
                {
                    IntPtr rootHwnd = GetAncestor(hitHwnd, GA_ROOT);
                    cursorOverDesktop = (rootHwnd != IntPtr.Zero && rootHwnd == _desktopHwnd) ||
                                        IsDesktopWindow(rootHwnd) ||
                                        IsDesktopWindow(hitHwnd);
                }
            }

            _isOverlayInteractive = cursorOverDesktop;

            if (cursorOverDesktop)
            {
                _isDesktopOccluded = false;
                return;
            }

            IntPtr currentFg = GetForegroundWindow();
            _isDesktopOccluded = currentFg != IntPtr.Zero &&
                                 currentFg != _desktopHwnd &&
                                 !IsDesktopWindow(currentFg) &&
                                 !IsIconic(currentFg) &&
                                 IsZoomed(currentFg);
        }

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

        private static bool IsDesktopWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;

            StringBuilder sb = new StringBuilder(64);
            GetClassName(hwnd, sb, sb.Capacity);
            string cls = sb.ToString();
            return cls == "Progman" || cls == "WorkerW" || cls == "SHELLDLL_DefView" || cls == "SysListView32";
        }
    }
}
