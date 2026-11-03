using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using SpiderDesk.Core;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Pencerenin tıklama geçirgenliğini (Click-Through) ve Masaüstü Z-Sırasını (Z-Order) yönetir (SRP & DIP).
    /// Ekran yenilenmesi/titremesi (flicker) olmaması için SetWindowPos yalnızca durum değiştiğinde çağrılır.
    /// </summary>
    public class WindowLayerService : IWindowLayerService
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOSENDCHANGING = 0x0400;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private IntPtr _hwnd;
        private IntPtr _lastForegroundHwnd;
        private bool? _lastAppliedTopmostState;
        private bool _alwaysOnTop;

        /// <summary>
        /// Kullanıcı masaüstüne bakıyorsa veya "Pencerelerin Üstünde Göster" açıksa true döner.
        /// Tarayıcı vb. pencerelerde gezinirken false döner ki fare hareketleri arka plandaki örümceği etkilemesin.
        /// </summary>
        public bool IsOverlayInteractive => _lastAppliedTopmostState == true;

        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                if (_alwaysOnTop != value)
                {
                    _alwaysOnTop = value;
                    _lastAppliedTopmostState = null; // Değişikliği hemen uygula
                    EnforceZOrder();
                }
            }
        }

        public void Attach(Window window)
        {
            _hwnd = new WindowInteropHelper(window).Handle;
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
            _lastAppliedTopmostState = null;
            EnforceZOrder();
        }

        public void EnforceZOrder()
        {
            if (_hwnd == IntPtr.Zero) return;

            IntPtr currentFg = GetForegroundWindow();
            // Ön plandaki pencere değişmediyse ve zaten durum uygulandıysa Windows API'yi tekrar çağırma (F5 titremesini önler)
            if (currentFg == _lastForegroundHwnd && _lastAppliedTopmostState.HasValue)
            {
                return;
            }

            _lastForegroundHwnd = currentFg;
            bool shouldBeTopmost = _alwaysOnTop || IsDesktopWindow(currentFg);

            // Sadece Z-Order durumu gerçekten değiştiğinde SetWindowPos çağır
            if (_lastAppliedTopmostState == shouldBeTopmost)
            {
                return;
            }

            _lastAppliedTopmostState = shouldBeTopmost;
            const uint flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOSENDCHANGING;

            if (shouldBeTopmost)
            {
                SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, flags);
            }
            else
            {
                SetWindowPos(_hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, flags);
                SetWindowPos(_hwnd, HWND_BOTTOM, 0, 0, 0, 0, flags);
            }
        }

        private static bool IsDesktopWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;

            StringBuilder sb = new StringBuilder(64);
            GetClassName(hwnd, sb, sb.Capacity);
            string cls = sb.ToString();
            return cls == "Progman" || cls == "WorkerW";
        }
    }
}
