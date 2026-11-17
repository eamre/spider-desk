using SpiderDesk.Domain.Performance;
using SpiderDesk.Infrastructure.Windowing;

namespace SpiderDesk.Tests.Infrastructure.Windowing
{
    /// <summary>
    /// Testler için sahte bir Windows masaüstü. Pencereleri üstten alta doğru basit bir listede tutar ve
    /// gerçek Windows'un temel kurallarını taklit eder: "her zaman üstte" pencereler hep normal pencerelerin üstündedir,
    /// masaüstü (duvar kâğıdı ve ikonlar) en alttadır. Testler sonuca bakar: katman nerede duruyor?
    /// </summary>
    internal sealed class FakeDesktop : IDesktopWindows, IWindowInspector
    {
        private sealed class FakeWindow
        {
            public required IntPtr Handle { get; init; }
            public required string Name { get; init; }
            public bool IsDesktop { get; init; }
            public bool IsTopmost { get; set; }
            public bool IsFullscreen { get; set; }
            public bool IsMinimized { get; set; }
        }

        private readonly List<FakeWindow> _zOrder = new(); // 0 = en üstteki pencere
        private int _nextHandle = 100;

        /// <summary>Duvar kâğıdını ve ikonları taşıyan pencere.</summary>
        public IntPtr Desktop { get; private set; }

        /// <summary>Fare imlecinin üstünde durduğu pencere.</summary>
        public IntPtr CursorOver { get; set; }

        /// <summary>Kullanıcının o an çalıştığı (öndeki) pencere.</summary>
        public IntPtr Foreground { get; set; }

        /// <summary>Pencerelerin sırasını değiştiren çağrıların sayısı (gerçekte her biri bir SetWindowPos'tur).</summary>
        public int ZOrderChanges { get; private set; }

        /// <summary>"İmlecin altında ne var?" sorusunun kaç kez sorulduğu.</summary>
        public int CursorQueries { get; private set; }

        public List<IntPtr> ClickThroughWindows { get; } = new();

        public FakeDesktop()
        {
            Desktop = AddAtBottom("Masaüstü", isDesktop: true);
            CursorOver = Desktop;
        }

        // ---------- Testlerin masaüstünü kurması için ----------

        public IntPtr OpenApp(string name, bool fullscreen = false, bool topmost = false)
        {
            var window = new FakeWindow { Handle = NewHandle(), Name = name, IsFullscreen = fullscreen, IsTopmost = topmost };
            _zOrder.Insert(TopOfGroup(window.IsTopmost), window);
            return window.Handle;
        }

        public void Minimize(IntPtr hwnd) => Find(hwnd)!.IsMinimized = true;

        /// <summary>Başka bir program katmanı masaüstünün arkasına iter.</summary>
        public void PushBehindDesktop(IntPtr hwnd)
        {
            FakeWindow window = Find(hwnd)!;
            _zOrder.Remove(window);
            _zOrder.Insert(IndexOf(Desktop) + 1, window);
        }

        /// <summary>Explorer yeniden başlar: eski masaüstü penceresi kaybolur, yenisi açılır.</summary>
        public void RestartExplorer()
        {
            _zOrder.Remove(Find(Desktop)!);
            Desktop = AddAtBottom("Yeni masaüstü", isDesktop: true);
        }

        // ---------- Testlerin sonucu okuması için ----------

        public bool IsDirectlyAboveDesktop(IntPtr hwnd) => IndexOf(hwnd) == IndexOf(Desktop) - 1;

        public bool IsAtTheVeryTop(IntPtr hwnd) => IndexOf(hwnd) == 0;

        public bool IsMarkedTopmost(IntPtr hwnd) => Find(hwnd)!.IsTopmost;

        /// <summary>Pencerelerin üstten alta sırası; başarısız testin mesajında okunur.</summary>
        public string Order => string.Join(" > ", _zOrder.Select(w => w.Name));

        // ---------- IDesktopWindows ----------

        public void MakeClickThrough(IntPtr hwnd) => ClickThroughWindows.Add(hwnd);

        public IntPtr FindDesktopHost() => Desktop;

        public IntPtr WindowAbove(IntPtr hwnd)
        {
            int index = IndexOf(hwnd);
            return index > 0 ? _zOrder[index - 1].Handle : IntPtr.Zero;
        }

        public IntPtr WindowBelow(IntPtr hwnd)
        {
            int index = IndexOf(hwnd);
            return index >= 0 && index < _zOrder.Count - 1 ? _zOrder[index + 1].Handle : IntPtr.Zero;
        }

        public bool IsTopmost(IntPtr hwnd) => Find(hwnd)?.IsTopmost ?? false;

        public void SetTopmost(IntPtr hwnd, bool topmost)
        {
            ZOrderChanges++;
            FakeWindow window = Find(hwnd)!;

            // Windows'ta olduğu gibi: zaten normal olan bir pencereyi "normal yap" demek onu yerinden oynatmaz
            if (!topmost && !window.IsTopmost) return;

            window.IsTopmost = topmost;
            MoveTo(window, TopOfGroupWithout(window));
        }

        public void PlaceBehind(IntPtr hwnd, IntPtr windowAbove)
        {
            ZOrderChanges++;
            FakeWindow window = Find(hwnd)!;
            _zOrder.Remove(window);
            _zOrder.Insert(IndexOf(windowAbove) + 1, window);
        }

        public void BringToTop(IntPtr hwnd)
        {
            ZOrderChanges++;
            FakeWindow window = Find(hwnd)!;
            MoveTo(window, TopOfGroupWithout(window));
        }

        public IntPtr ForegroundWindow() => Foreground;

        public IntPtr WindowUnderCursor(out IntPtr rootWindow)
        {
            CursorQueries++;
            rootWindow = CursorOver;
            return CursorOver;
        }

        // ---------- IWindowInspector ----------

        public bool IsDesktopWindow(IntPtr hwnd) => Find(hwnd)?.IsDesktop ?? false;

        public WindowSnapshot Capture(IntPtr hwnd, IntPtr overlayHwnd)
        {
            if (Find(hwnd) is not { } window) return default;
            return new WindowSnapshot(
                IsDesktop: window.IsDesktop,
                IsMinimized: window.IsMinimized,
                IsMaximized: window.IsFullscreen,
                CoversMonitor: window.IsFullscreen,
                IsOnOverlayMonitor: true);
        }

        // ---------- Yardımcılar ----------

        private IntPtr NewHandle() => new(_nextHandle++);

        private IntPtr AddAtBottom(string name, bool isDesktop)
        {
            var window = new FakeWindow { Handle = NewHandle(), Name = name, IsDesktop = isDesktop };
            _zOrder.Add(window);
            return window.Handle;
        }

        private FakeWindow? Find(IntPtr hwnd) => _zOrder.FirstOrDefault(w => w.Handle == hwnd);

        private int IndexOf(IntPtr hwnd) => _zOrder.FindIndex(w => w.Handle == hwnd);

        // "Her zaman üstte" grubunun en üstü 0'dır; normal grubun en üstü, ilk normal penceredir
        private int TopOfGroup(bool topmost) => topmost ? 0 : _zOrder.Count(w => w.IsTopmost);

        private int TopOfGroupWithout(FakeWindow window)
        {
            _zOrder.Remove(window);
            int index = TopOfGroup(window.IsTopmost);
            _zOrder.Add(window);
            return index;
        }

        private void MoveTo(FakeWindow window, int index)
        {
            _zOrder.Remove(window);
            _zOrder.Insert(index, window);
        }
    }
}
