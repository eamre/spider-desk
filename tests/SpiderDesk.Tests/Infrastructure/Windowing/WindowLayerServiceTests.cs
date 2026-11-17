using SpiderDesk.Infrastructure.Windowing;

namespace SpiderDesk.Tests.Infrastructure.Windowing
{
    /// <summary>
    /// Örümcek katmanının masaüstündeki yerini doğrular: normalde duvar kâğıdının hemen üstünde, bütün pencerelerin arkasında;
    /// "Pencerelerin Üstünde Göster" açıkken ya da bir duyuru/not gösterilirken en önde. Ayrıca farenin ne zaman çalıştığını,
    /// tam ekran bir uygulamanın fark edildiğini ve Windows'un gereksiz yere yorulmadığını doğrular.
    /// Gerçek Windows yerine <see cref="FakeDesktop"/> kullanılır.
    /// </summary>
    public class WindowLayerServiceTests
    {
        private readonly FakeDesktop _desktop = new();
        private readonly IntPtr _browser;
        private readonly IntPtr _editor;
        private readonly IntPtr _overlay;
        private readonly WindowLayerService _layer;

        public WindowLayerServiceTests()
        {
            _browser = _desktop.OpenApp("Tarayıcı");
            _editor = _desktop.OpenApp("Editör");
            // Örümceğin şeffaf penceresi gerçekte de bütün ekranı kaplar
            _overlay = _desktop.OpenApp("SpiderDesk", fullscreen: true);
            _layer = new WindowLayerService(_desktop, _desktop);
            _layer.Attach(_overlay);
        }

        // Uygulamadaki gibi her karede bir kez çağrılır; pencere sorguları 4 karede bir yapıldığı için 4 kare bir sorgu demektir
        private void Frames(int count)
        {
            for (int i = 0; i < count; i++) _layer.EnforceZOrder();
        }

        private void AssertSitsOnTheDesktop()
        {
            Assert.True(_desktop.IsDirectlyAboveDesktop(_overlay), $"Katman masaüstünün hemen üstünde değil: {_desktop.Order}");
            Assert.False(_desktop.IsMarkedTopmost(_overlay));
        }

        private void AssertInFrontOfEverything()
        {
            Assert.True(_desktop.IsAtTheVeryTop(_overlay), $"Katman en önde değil: {_desktop.Order}");
            Assert.True(_desktop.IsMarkedTopmost(_overlay));
        }

        // ---------- Yerleşim ----------

        [Fact]
        public void Attaching_MakesTheOverlayClickThrough()
        {
            Assert.Equal(new[] { _overlay }, _desktop.ClickThroughWindows);
        }

        [Fact]
        public void Normally_SitsDirectlyAboveTheDesktop_BehindAllOtherWindows()
        {
            AssertSitsOnTheDesktop();
        }

        [Fact]
        public void AlwaysOnTop_BringsItInFrontOfEverything()
        {
            _layer.AlwaysOnTop = true;

            AssertInFrontOfEverything();
        }

        [Fact]
        public void TurningAlwaysOnTopOff_SendsItBackAboveTheDesktop()
        {
            _layer.AlwaysOnTop = true;
            _layer.AlwaysOnTop = false;

            AssertSitsOnTheDesktop();
        }

        [Fact]
        public void ShowingANote_BringsItInFrontOfEverything_AndAfterwardsItGoesBack()
        {
            _layer.IsPresentingOnTop = true;
            AssertInFrontOfEverything();

            _layer.IsPresentingOnTop = false;
            AssertSitsOnTheDesktop();
        }

        [Fact]
        public void PushedBehindTheDesktop_ComesBackWithinFourFrames()
        {
            _desktop.PushBehindDesktop(_overlay);

            Frames(4);

            AssertSitsOnTheDesktop();
        }

        [Fact]
        public void PushedBehindTheDesktop_WithOnlyAnAlwaysOnTopWindowAbove_StillComesBack()
        {
            var desktop = new FakeDesktop();
            desktop.OpenApp("Yapışkan not", topmost: true);
            IntPtr overlay = desktop.OpenApp("SpiderDesk");
            var layer = new WindowLayerService(desktop, desktop);
            layer.Attach(overlay);
            desktop.PushBehindDesktop(overlay);

            for (int i = 0; i < 4; i++) layer.EnforceZOrder();

            Assert.True(desktop.IsDirectlyAboveDesktop(overlay), desktop.Order);
            Assert.False(desktop.IsMarkedTopmost(overlay));
        }

        [Fact]
        public void BeforeAttaching_DoesNothing()
        {
            var desktop = new FakeDesktop();
            var layer = new WindowLayerService(desktop, desktop);

            layer.EnforceZOrder();
            layer.AlwaysOnTop = true;
            layer.IsPresentingOnTop = true;

            Assert.Equal(0, desktop.ZOrderChanges);
            Assert.Equal(0, desktop.CursorQueries);
        }

        // ---------- Windows'u yormamak ----------

        [Fact]
        public void DoesNotMoveTheWindowEveryFrame_SoTheDesktopDoesNotFlicker()
        {
            int changesAfterAttach = _desktop.ZOrderChanges;
            Frames(200);
            Assert.Equal(changesAfterAttach, _desktop.ZOrderChanges);

            _layer.AlwaysOnTop = true;
            int changesAfterRaise = _desktop.ZOrderChanges;
            Frames(200);
            Assert.Equal(changesAfterRaise, _desktop.ZOrderChanges);
        }

        [Fact]
        public void AsksWindowsWhatIsUnderTheCursor_OnlyEveryFourthFrame()
        {
            int before = _desktop.CursorQueries;

            Frames(40);

            Assert.Equal(10, _desktop.CursorQueries - before);
        }

        // ---------- Fare ----------

        [Fact]
        public void MouseOverTheDesktop_ReachesTheSpider()
        {
            _desktop.CursorOver = _desktop.Desktop;
            Frames(4);

            Assert.True(_layer.IsOverlayInteractive);
        }

        [Fact]
        public void MouseOverAnotherWindow_DoesNotReachTheSpider()
        {
            _desktop.CursorOver = _browser;
            _desktop.Foreground = _browser;
            Frames(4);

            Assert.False(_layer.IsOverlayInteractive);
            Assert.False(_layer.IsDesktopOccluded);
        }

        [Fact]
        public void WhenTheSpiderIsInFront_TheMouseAlwaysReachesIt()
        {
            _desktop.CursorOver = _editor;

            _layer.AlwaysOnTop = true;
            Assert.True(_layer.IsOverlayInteractive);

            _layer.AlwaysOnTop = false;
            _layer.IsPresentingOnTop = true;
            Assert.True(_layer.IsOverlayInteractive);
        }

        [Fact]
        public void AfterExplorerRestarts_MouseOverTheNewDesktop_StillReachesTheSpider()
        {
            _desktop.RestartExplorer();
            _desktop.CursorOver = _desktop.Desktop;

            Frames(4);

            Assert.True(_layer.IsOverlayInteractive);
        }

        [Fact]
        public void AfterExplorerRestarts_ShowingANote_FindsTheNewDesktop()
        {
            _desktop.RestartExplorer();

            _layer.IsPresentingOnTop = true;
            _layer.IsPresentingOnTop = false;

            AssertSitsOnTheDesktop();
        }

        // ---------- Tam ekran uygulamalar ----------

        [Fact]
        public void FullscreenAppInFront_HidesTheSpider_UntilItIsMinimized()
        {
            IntPtr video = _desktop.OpenApp("Video", fullscreen: true);
            _desktop.Foreground = video;
            _desktop.CursorOver = video;
            Frames(4);
            Assert.True(_layer.IsDesktopOccluded);

            _desktop.Minimize(video);
            _desktop.Foreground = IntPtr.Zero;
            _desktop.CursorOver = _desktop.Desktop;
            Frames(4);
            Assert.False(_layer.IsDesktopOccluded);
        }

        [Fact]
        public void TheSpidersOwnFullscreenWindow_NeverCountsAsCoveringTheDesktop()
        {
            _desktop.Foreground = _overlay;
            _desktop.CursorOver = _overlay;

            Frames(4);

            Assert.False(_layer.IsDesktopOccluded);
        }

        [Fact]
        public void ShowingANoteOverAFullscreenApp_KeepsDrawing_ButHidesTheWebsBehindTheNote()
        {
            IntPtr video = _desktop.OpenApp("Video", fullscreen: true);
            _desktop.Foreground = video;
            _desktop.CursorOver = video;

            _layer.IsPresentingOnTop = true;

            AssertInFrontOfEverything();
            Assert.False(_layer.IsDesktopOccluded); // örümcek ve not çizilmeye devam eder
            Assert.True(_layer.IsDesktopCovered);   // ağlar ve böcekler videonun üstünde görünmesin diye gizlenir
            Assert.True(_layer.IsOverlayInteractive);
        }

        [Fact]
        public void AlwaysOnTop_OverAFullscreenApp_ShowsEverything()
        {
            IntPtr video = _desktop.OpenApp("Video", fullscreen: true);
            _desktop.Foreground = video;
            _desktop.CursorOver = video;

            _layer.AlwaysOnTop = true;
            Frames(8);

            Assert.False(_layer.IsDesktopOccluded);
            Assert.False(_layer.IsDesktopCovered);
        }
    }
}
