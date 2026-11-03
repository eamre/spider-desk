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
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = false;
            ShowInTaskbar = false;
            WindowState = WindowState.Maximized;
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

            _trayMenuService.StartupChanged += enabled => _startupService.SetEnabled(enabled);
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
            double dt = Math.Min(0.1, (now - _lastTick).TotalSeconds);
            _lastTick = now;

            _windowLayerService.EnforceZOrder();

            if (!_isPaused)
            {
                double width = ActualWidth > 100 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
                double height = ActualHeight > 100 ? ActualHeight : SystemParameters.PrimaryScreenHeight;

                _cursorService.Update(this, dt);

                double effectiveCursorSpeed = _windowLayerService.IsOverlayInteractive
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

        protected override void OnClosed(EventArgs e)
        {
            _trayMenuService.Dispose();
            base.OnClosed(e);
        }
    }
}
