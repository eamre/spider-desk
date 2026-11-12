using System;
using System.Runtime.InteropServices;
using System.Text;
using SpiderDesk.Domain;

namespace SpiderDesk.Infrastructure.Windowing
{
    /// <summary>
    /// Win32 API üzerinden pencere bilgilerini okuyup işletim sisteminden bağımsız <see cref="WindowSnapshot"/>'a çevirir.
    /// Yalnızca veri toplar, karar vermez; kararlar <see cref="DesktopOcclusionPolicy"/> gibi saf kural sınıflarındadır.
    /// Paylaşımlı sınıf adı tamponu kullandığı için yalnızca UI iş parçacığından çağrılmalıdır.
    /// </summary>
    public sealed class Win32WindowInspector : IWindowInspector
    {
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private readonly StringBuilder _classNameBuffer = new(64);

        public bool IsDesktopWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;

            _classNameBuffer.Clear();
            GetClassName(hwnd, _classNameBuffer, _classNameBuffer.Capacity);
            string cls = _classNameBuffer.ToString();
            return cls == "Progman" || cls == "WorkerW" || cls == "SHELLDLL_DefView" || cls == "SysListView32";
        }

        public WindowSnapshot Capture(IntPtr hwnd, IntPtr overlayHwnd)
        {
            if (hwnd == IntPtr.Zero) return default;

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            IntPtr overlayMonitor = MonitorFromWindow(overlayHwnd, MONITOR_DEFAULTTONEAREST);

            return new WindowSnapshot(
                IsDesktop: IsDesktopWindow(hwnd),
                IsMinimized: IsIconic(hwnd),
                IsMaximized: IsZoomed(hwnd),
                CoversMonitor: CoversMonitor(hwnd, monitor),
                IsOnOverlayMonitor: monitor != IntPtr.Zero && monitor == overlayMonitor);
        }

        private static bool CoversMonitor(IntPtr hwnd, IntPtr monitor)
        {
            if (monitor == IntPtr.Zero || !GetWindowRect(hwnd, out RECT window)) return false;

            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, ref info)) return false;

            RECT screen = info.rcMonitor;
            return window.Left <= screen.Left
                && window.Top <= screen.Top
                && window.Right >= screen.Right
                && window.Bottom >= screen.Bottom;
        }
    }
}
