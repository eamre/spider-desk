using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SpiderDesk.Core;
using SpiderDesk.Infrastructure;
using SpiderDesk.Rendering;

namespace SpiderDesk
{
    /// <summary>
    /// Tüm bağımlılıklarını dışarıdan (Constructor Injection) alan ana katman penceresi (DIP & SRP).
    /// </summary>
    public class MainWindow : Window
    {
        private readonly SimulationEngine _engine;
        private readonly SimulationCanvas _canvas;
        private readonly WebRenderer _webRenderer;
        private readonly IWindowLayerService _windowLayerService;
        private readonly ICursorTrackingService _cursorService;
        private readonly IStartupRegistrationService _startupService;
        private readonly TrayMenuService _trayMenuService;
        private readonly DispatcherTimer _timer;

        private static readonly TimeSpan ActiveFrameInterval = TimeSpan.FromMilliseconds(16);  // 60 FPS (Masaüstü görünürken)
        private static readonly TimeSpan OccludedEcoInterval = TimeSpan.FromMilliseconds(50);  // 20 FPS (Tam ekran pencere arkasındayken düşük güç modu)

        private bool _isPaused;
        private DateTime _lastTick;

        public MainWindow(
            SimulationEngine engine,
            SimulationCanvas canvas,
            WebRenderer webRenderer,
            IWindowLayerService windowLayerService,
            ICursorTrackingService cursorService,
            IStartupRegistrationService startupService,
            TrayMenuService trayMenuService)
        {
            _engine = engine;
            _canvas = canvas;
            _webRenderer = webRenderer;
            _windowLayerService = windowLayerService;
            _cursorService = cursorService;
            _startupService = startupService;
            _trayMenuService = trayMenuService;

            ConfigureOverlayWindow();
            Content = _canvas;
            BindTrayEvents();

            _lastTick = DateTime.Now;
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _timer.Tick += OnFrameTick;
            _timer.Start();
        }

        private void ConfigureOverlayWindow()
        {
            Title = "SpiderDesk";
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = false;
            ShowInTaskbar = false;
            WindowState = WindowState.Normal;
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            // Tam ekran (Maximized / 100% boy) pencereler Windows Explorer'da ABN_FULLSCREENAPP bildirimi
            // tetikleyip masaüstü ikonlarının F5 yapılmış gibi yenilenmesine yol açtığı için 1 piksel kısa tutulur.
            Height = Math.Max(100, SystemParameters.PrimaryScreenHeight - 1);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowLayerService.Attach(this);
        }

        private void BindTrayEvents()
        {
            _trayMenuService.NewWebRequested += () => _engine.CommandNewWeb();
            _trayMenuService.RappelRequested += () => _engine.CommandRappel();
            _trayMenuService.RainOnWebsRequested += () => _engine.CommandRainOnWebs();
            _trayMenuService.SpawnBugsRequested += count => _engine.SpawnExtraBugs(count);
            _trayMenuService.ClearWebsRequested += () => _engine.ClearWebs();

            _trayMenuService.ThemeSelected += theme =>
            {
                _engine.SetTheme(theme);
                _webRenderer.ApplyTheme(theme);
                _canvas.InvalidateVisual();
            };

            _trayMenuService.SpawnRateSelected += rate => _engine.SetSpawnRate(rate);
            _trayMenuService.DewSpawnRateSelected += rate => _engine.SetDewSpawnRate(rate);
            _trayMenuService.SpeedMultiplierSelected += mult => _engine.SetSpeedMultiplier(mult);
            _trayMenuService.TimeModeSelected += mode => _engine.SetTimeMode(mode);

            _trayMenuService.AlwaysOnTopChanged += alwaysOnTop =>
            {
                _windowLayerService.AlwaysOnTop = alwaysOnTop;
                _windowLayerService.EnforceZOrder();
                _engine.SetAlwaysOnTopPreference(alwaysOnTop);
            };

            _trayMenuService.StartupChanged += enabled =>
            {
                _startupService.SetEnabled(enabled);
                _engine.SetRunOnStartupPreference(enabled);
            };
            _trayMenuService.PauseChanged += paused => _isPaused = paused;
            _trayMenuService.ExitRequested += () =>
            {
                _trayMenuService.Dispose();
                Application.Current.Shutdown();
            };
        }

        private void OnFrameTick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double dt = Math.Min(0.15, (now - _lastTick).TotalSeconds);
            _lastTick = now;

            _windowLayerService.EnforceZOrder();
            UpdateAdaptiveFrameRate();

            if (!_isPaused)
            {
                double width = ActualWidth > 100 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
                double height = ActualHeight > 100 ? ActualHeight : SystemParameters.PrimaryScreenHeight;

                bool isInteractive = _windowLayerService.IsOverlayInteractive;
                bool isOccluded = _windowLayerService.IsDesktopOccluded;

                if (isInteractive)
                {
                    _cursorService.Update(this, dt);
                }

                double effectiveCursorSpeed = isInteractive
                    ? _cursorService.Speed
                    : 0.0;

                _engine.Update(
                    dt,
                    width,
                    height,
                    _cursorService.CurrentPosition,
                    _cursorService.PreviousPosition,
                    effectiveCursorSpeed
                );

                _canvas.InvalidateVisual();

                _trayMenuService.UpdateStatus(
                    _engine.Spider.Level,
                    _engine.Spider.Metabolism.SatietyPercent,
                    _engine.Spider.CurrentState.DisplayName,
                    _engine.Spider.BugsCaught
                );
            }
        }

        private void UpdateAdaptiveFrameRate()
        {
            // Masaüstü kısmen veya tamamen görünür olduğu her an 60 FPS çalışır;
            // yalnızca tam ekran büyütülmüş bir pencere masaüstünü tamamen kapattığında 10 Hz Eco moduna geçer.
            TimeSpan desiredInterval = _windowLayerService.IsDesktopOccluded
                ? OccludedEcoInterval
                : ActiveFrameInterval;

            if (_timer.Interval != desiredInterval)
            {
                _timer.Interval = desiredInterval;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayMenuService.Dispose();
            base.OnClosed(e);
        }
    }
}
