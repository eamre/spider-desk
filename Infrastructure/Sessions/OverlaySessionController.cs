using System;
using System.Windows;
using System.Windows.Threading;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Overlay;
using SpiderDesk.Infrastructure.Tray;
using SpiderDesk.Infrastructure.Windowing;
using SpiderDesk.Rendering;

namespace SpiderDesk.Infrastructure.Sessions
{
    /// <summary>
    /// Simülasyon döngüsü zamanlayıcısını (DispatcherTimer), adaptif kare hızı (FPS) yönetimini
    /// ve tepsideki Duraklat ile Çıkış'ı koordine eder. Her karede Pomodoro'ya, hatırlatıcılara ve "Örümcek" sekmesine saati verir.
    /// Örümceğe verilen komutlar <see cref="SpiderSessionController"/>'da, kayıtlı ayarlar <see cref="SettingsSessionController"/>'dadır.
    /// </summary>
    public sealed class OverlaySessionController : IDisposable
    {
        private readonly Window _hostWindow;
        private readonly SimulationEngine _engine;
        private readonly SimulationCanvas _canvas;
        private readonly IWindowLayerService _windowLayerService;
        private readonly ICursorTrackingService _cursorService;
        private readonly TrayMenuService _trayMenuService;
        private readonly PomodoroSessionController _pomodoro;
        private readonly ReminderSessionController _reminders;
        private readonly SpiderSessionController _spider;
        private readonly DispatcherTimer _timer;
        private readonly AnnouncementCurtain _curtain = new();

        private bool _isPaused;
        private bool _wasOccluded;
        private bool _disposed;
        private DateTime _lastTick;

        public OverlaySessionController(
            Window hostWindow,
            SimulationEngine engine,
            SimulationCanvas canvas,
            IWindowLayerService windowLayerService,
            ICursorTrackingService cursorService,
            TrayMenuService trayMenuService,
            PomodoroSessionController pomodoro,
            ReminderSessionController reminders,
            SpiderSessionController spider)
        {
            _hostWindow = hostWindow;
            _engine = engine;
            _canvas = canvas;
            _windowLayerService = windowLayerService;
            _cursorService = cursorService;
            _trayMenuService = trayMenuService;
            _pomodoro = pomodoro;
            _reminders = reminders;
            _spider = spider;

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

            // Pomodoro ve hatırlatıcı gerçek saate bakar; simülasyon duraklatılmış ya da ekran kapalı olsa bile süre işlemeye devam eder
            _pomodoro.Tick(now);
            _reminders.Tick(now);

            // Örümcek bir duyuru yazarken ya da not gösterirken katman tam ekran uygulamaların bile üstüne çıkar.
            // Önce sahne hazırlanır, katman bir kare sonra öne alınır; sıralamanın kuralları AnnouncementCurtain'dadır.
            CurtainStep curtain = _curtain.Next(_engine.IsShowingAnnouncement, _isPaused, _windowLayerService.IsDesktopCovered);
            _canvas.SetBackgroundHidden(curtain.HideBackground);
            if (curtain.RedrawEverything)
            {
                _canvas.RefreshLayers(forceAll: true);
            }
            _windowLayerService.IsPresentingOnTop = curtain.RaiseToTop;
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

            _spider.ShowStatus();
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
