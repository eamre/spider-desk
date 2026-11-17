using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SpiderDesk.Infrastructure.Windowing
{
    /// <summary>
    /// Win32DesktopWindows, Win32WindowInspector ve CursorTrackingService'in kullandığı Windows (user32.dll) fonksiyonları,
    /// sabitleri ve yapıları.
    /// Burada hiçbir karar verilmez; yalnızca Windows'a açılan kapı tanımlanır. Kullanan sınıflar bunları
    /// <c>using static</c> ile doğrudan adlarıyla çağırır. Adlar Windows belgelerindekiyle aynı bırakılmıştır
    /// (örneğin <c>SWP_NOMOVE</c>), böylece belgelerde kolayca aranabilir.
    /// </summary>
    internal static class NativeMethods
    {
        // ---------- Pencere stilleri (GetWindowLong / SetWindowLong) ----------

        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_TOPMOST = 0x00000008;
        internal const int WS_EX_TRANSPARENT = 0x00000020;   // Tıklamalar pencerenin içinden geçer
        internal const int WS_EX_TOOLWINDOW = 0x00000080;    // Görev çubuğunda ve Alt+Tab'da görünmez
        internal const int WS_EX_NOACTIVATE = 0x08000000;    // Tıklanınca öne gelip odağı çalmaz

        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        // ---------- Z-sırası (SetWindowPos / GetWindow) ----------

        internal const uint GW_HWNDNEXT = 2;   // Z-sırasında bir alttaki pencere
        internal const uint GW_HWNDPREV = 3;   // Z-sırasında bir üstteki pencere

        internal static readonly IntPtr HWND_TOP = new IntPtr(0);
        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        internal static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOREDRAW = 0x0008;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const uint SWP_NOSENDCHANGING = 0x0400;
        internal const uint SWP_DEFERERASE = 0x2000;

        [DllImport("user32.dll")]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        // ---------- Pencere bulma ----------

        internal const uint GA_ROOT = 2;   // Bir alt pencerenin en üstteki ana penceresi

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        internal static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        // ---------- Pencere durumu ve monitör ----------

        internal const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        internal static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

        [DllImport("user32.dll")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        // ---------- Fare imleci ----------

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        internal static extern bool GetCursorPos(out POINT lpPoint);
    }
}
