using System;
using System.Windows;
using System.Windows.Threading;
using SpiderDesk.Core;
using SpiderDesk.Core.Services;
using SpiderDesk.Domain;
using SpiderDesk.Rendering;
using SpiderDesk.Rendering.Web;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Simülasyon döngüsü zamanlayıcısını (DispatcherTimer), adaptif kare hızı (FPS) yönetimini
    /// ve sistem tepsisi (Tray) menü komutlarının simülasyona bağlanmasını koordine eder.
    /// </summary>
    public sealed class OverlaySessionController : IDisposable
    {
        private readonly Window _hostWindow;
        private readonly SimulationEngine _engine;
        private readonly SimulationCanvas _canvas;
        private readonly WebRenderer _webRenderer;
        private readonly IWindowLayerService _windowLayerService;
        private readonly ICursorTrackingService _cursorService;
        private readonly IStartupRegistrationService _startupService;
        private readonly TrayMenuService _trayMenuService;
        private readonly DispatcherTimer _timer;

        private bool _isPaused;
        private bool _wasOccluded;
        private bool _disposed;
        private DateTime _lastTick;

        public OverlaySessionController(
            Window hostWindow,
            SimulationEngine engine,
            SimulationCanvas canvas,
            WebRenderer webRenderer,
            IWindowLayerService windowLayerService,
            ICursorTrackingService cursorService,
            IStartupRegistrationService startupService,
            TrayMenuService trayMenuService)
        {
            _hostWindow = hostWindow;
            _engine = engine;
            _canvas = canvas;
            _webRenderer = webRenderer;
            _windowLayerService = windowLayerService;
            _cursorService = cursorService;
            _startupService = startupService;
            _trayMenuService = trayMenuService;

            BindTrayEvents();

            _lastTick = DateTime.Now;
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = FrameRatePolicy.ActiveFrameInterval
            };
            _timer.Tick += OnFrameTick;
        }

        public void Start() => _timer.Start();

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
                _canvas.RefreshLayers(forceAll: true);
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
                Dispose();
                Application.Current.Shutdown();
            };
        }

        private void OnFrameTick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double dt = Math.Min(0.15, (now - _lastTick).TotalSeconds);
            _lastTick = now;

            _windowLayerService.EnforceZOrder();

            bool isInteractive = _windowLayerService.IsOverlayInteractive;
            bool isOccluded = _windowLayerService.IsDesktopOccluded;
            bool hasActivity = _engine.HasVisualActivity;

            // Kare hızı ve render kararlarını saf kural sınıfına (FrameRatePolicy) devret
            var decision = FrameRatePolicy.Decide(_isPaused, isOccluded, hasActivity);

            if (_timer.Interval != decision.TargetInterval)
            {
                _timer.Interval = decision.TargetInterval;
            }

            if (!decision.ShouldUpdateEngine)
            {
                return;
            }

            double width = _hostWindow.ActualWidth > 100 ? _hostWindow.ActualWidth : SystemParameters.PrimaryScreenWidth;
            double height = _hostWindow.ActualHeight > 100 ? _hostWindow.ActualHeight : SystemParameters.PrimaryScreenHeight;

            if (isInteractive)
            {
                _cursorService.Update(_hostWindow, dt);
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

            // Tam ekran bir uygulama öndeyken veya render kapalıyken:
            // Simülasyon (ağ örme, avlanma, sayaçlar) arka planda kesintisiz çalışır,
            // ancak görünmeyen ekranı GPU'ya çizdirmeyerek %0 GPU kullanımı sağlanır.
            if (decision.ShouldRenderCanvas)
            {
                _canvas.RefreshLayers(forceAll: _wasOccluded);
            }

            _wasOccluded = isOccluded;

            _trayMenuService.UpdateStatus(
                _engine.Spider.Level,
                _engine.Spider.Metabolism.SatietyPercent,
                _engine.Spider.CurrentState.DisplayName,
                _engine.Spider.BugsCaught
            );
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Stop();
            _timer.Tick -= OnFrameTick;
            _trayMenuService.Dispose();
        }
    }
}
