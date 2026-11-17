using System;
using static SpiderDesk.Infrastructure.Windowing.NativeMethods;

namespace SpiderDesk.Infrastructure.Windowing
{
    /// <summary>
    /// <see cref="IDesktopWindows"/>'un gerçek Windows uygulaması. Her metot bir ya da birkaç user32 çağrısıdır;
    /// karar vermez, kararlar <see cref="WindowLayerService"/>'tedir.
    /// </summary>
    public sealed class Win32DesktopWindows : IDesktopWindows
    {
        // SWP_NOREDRAW ve SWP_DEFERERASE: Z-sırası değişirken Windows'un alttaki masaüstü ikon listesini
        // (SysListView32) silip baştan boyamasını (F5 yenilenme hissini) tamamen engeller.
        private const uint ZOrderOnlyFlags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOSENDCHANGING | SWP_NOREDRAW | SWP_DEFERERASE;

        public void MakeClickThrough(IntPtr hwnd)
        {
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        }

        public IntPtr FindDesktopHost()
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

        public IntPtr WindowAbove(IntPtr hwnd) => GetWindow(hwnd, GW_HWNDPREV);

        public IntPtr WindowBelow(IntPtr hwnd) => GetWindow(hwnd, GW_HWNDNEXT);

        public bool IsTopmost(IntPtr hwnd) => (GetWindowLong(hwnd, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0;

        public void SetTopmost(IntPtr hwnd, bool topmost) =>
            SetWindowPos(hwnd, topmost ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, ZOrderOnlyFlags);

        public void PlaceBehind(IntPtr hwnd, IntPtr windowAbove) =>
            SetWindowPos(hwnd, windowAbove, 0, 0, 0, 0, ZOrderOnlyFlags);

        public void BringToTop(IntPtr hwnd) =>
            SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, ZOrderOnlyFlags);

        public IntPtr ForegroundWindow() => GetForegroundWindow();

        public IntPtr WindowUnderCursor(out IntPtr rootWindow)
        {
            rootWindow = IntPtr.Zero;
            if (!GetCursorPos(out POINT point)) return IntPtr.Zero;

            IntPtr hit = WindowFromPoint(point);
            if (hit != IntPtr.Zero)
            {
                rootWindow = GetAncestor(hit, GA_ROOT);
            }
            return hit;
        }
    }
}
