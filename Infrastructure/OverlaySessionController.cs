using System;
using System.Windows;
using System.Windows.Threading;
using SpiderDesk.Core.Services;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Performance;
using SpiderDesk.Infrastructure.Tray;
using SpiderDesk.Rendering;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Simülasyon döngüsü zamanlayıcısını (DispatcherTimer), adaptif kare hızı (FPS) yönetimini
    /// ve tepsideki anlık komutların (yeni ağ, yağmur, böcek, Duraklat, Çıkış) simülasyona bağlanmasını koordine eder.
    /// Kayıtlı ayarlar <see cref="SettingsSessionController"/>'da, Pomodoro ve hatırlatıcılar kendi denetleyicilerindedir.
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
        private readonly DispatcherTimer _timer;

        private bool _isPaused;
        private bool _wasOccluded;
        private bool _wasAnnouncingLastFrame;
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
            ReminderSessionController reminders)
        {
            _hostWindow = hostWindow;
            _engine = engine;
            _canvas = canvas;
            _windowLayerService = windowLayerService;
            _cursorService = cursorService;
            _trayMenuService = trayMenuService;
            _pomodoro = pomodoro;
            _reminders = reminders;

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

            // Örümcek bir duyuru (FOCUS/BREAK) yazarken ya da bir not gösterirken katman tam ekran uygulamaların bile üstüne çıkar.
            // Duraklatılmışsa çıkmaz; yoksa yarım kalan yazı uygulamaların üstünde asılı kalırdı.
            bool isAnnouncing = !_isPaused && _engine.IsShowingAnnouncement;

            // 1. Önce sahneyi hazırla: bir uygulamanın üstünde yazılacaksa ağları ve böcekleri gizle, örümceği güncel yerine çiz.
            //    Katman bu sırada hâlâ uygulamanın arkasında olduğu için bunu kimse görmez.
            //    Masaüstündeyken ağlar zaten görünür olduğu için gizlenmez.
            _canvas.SetBackgroundHidden(isAnnouncing && _windowLayerService.IsDesktopCovered);
            if (isAnnouncing && !_wasAnnouncingLastFrame)
            {
                _canvas.RefreshLayers(forceAll: true);
            }

            // 2. Sonra perdeyi aç: katman, hazırlanan görüntü çizildikten bir kare sonra öne alınır.
            //    Aynı karede öne alınsaydı, arkada bekleyen eski görüntü (ağlar, böcekler) bir an görünürdü.
            _windowLayerService.IsPresentingOnTop = isAnnouncing && _wasAnnouncingLastFrame;
            _wasAnnouncingLastFrame = isAnnouncing;
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
