using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SpiderDesk.Core;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Infrastructure;
using SpiderDesk.Rendering;

namespace SpiderDesk
{
    /// <summary>
    /// Uygulamanın Composition Root (Bağımlılık Enjeksiyonu Başlangıç Noktası) ve global hata yakalama sınıfı.
    /// </summary>
    public partial class App : Application
    {
        [DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hwProc);

        private Mutex? _singleInstanceMutex;
        private bool _ownsMutex;
        private OverlaySessionController? _sessionController;

        protected override void OnStartup(StartupEventArgs e)
        {
            RegisterGlobalExceptionHandlers();

            _singleInstanceMutex = new Mutex(true, "SpiderDesk_SingleInstance_Mutex", out _ownsMutex);
            if (!_ownsMutex)
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);

            // Altyapı servislerinin oluşturulması
            IWindowLayerService windowLayerService = new WindowLayerService();
            ICursorTrackingService cursorService = new CursorTrackingService();
            IStartupRegistrationService startupService = new StartupRegistrationService();
            IEnvironmentService environmentService = new SystemEnvironmentService();
            ISettingsRepository settingsRepository = new JsonSettingsRepository();

            // Domain ve Görselleştirme bileşenlerinin oluşturulması
            IBugSpeciesProvider speciesProvider = new BugSpeciesCatalog();
            var engine = new SimulationEngine(speciesProvider, environmentService, settingsRepository);

            if (startupService.IsEnabled() != engine.Settings.RunOnStartup)
            {
                startupService.SetEnabled(engine.Settings.RunOnStartup);
            }

            windowLayerService.AlwaysOnTop = engine.Settings.AlwaysOnTop;

            var webRenderer = new WebRenderer();
            webRenderer.ApplyTheme(engine.Spider.Theme);

            var canvas = new SimulationCanvas(
                engine.World,
                engine.Spider,
                webRenderer,
                new BugRenderer(),
                new SpiderRenderer()
            );

            var trayMenuService = new TrayMenuService(
                currentSettings: engine.Settings,
                initialStartupEnabled: startupService.IsEnabled()
            );

            var mainWindow = new MainWindow(canvas, windowLayerService);
            _sessionController = new OverlaySessionController(
                mainWindow,
                engine,
                canvas,
                webRenderer,
                windowLayerService,
                cursorService,
                startupService,
                trayMenuService
            );

            mainWindow.Closed += (_, _) => _sessionController.Dispose();
            mainWindow.Show();
            _sessionController.Start();

            SchedulePostStartupMemoryTrim();
        }

        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += (_, args) =>
            {
                AppDiagnostics.LogError("DispatcherUnhandledException", args.Exception);
                // Şeffaf masaüstü katmanında geçici bir çizim/Win32 hatası olduğunda uygulamanın
                // sessizce çökmesini önle; hatayı logla ve döngüye devam et.
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    AppDiagnostics.LogError("AppDomain.UnhandledException", ex);
                }
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                AppDiagnostics.LogError("TaskScheduler.UnobservedTaskException", args.Exception);
                args.SetObserved();
            };
        }

        private static void SchedulePostStartupMemoryTrim()
        {
            var trimTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(2.5)
            };

            trimTimer.Tick += (_, _) =>
            {
                trimTimer.Stop();
                try
                {
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
                    GC.WaitForPendingFinalizers();
                    EmptyWorkingSet(Process.GetCurrentProcess().Handle);
                }
                catch (Exception ex)
                {
                    AppDiagnostics.LogWarning("MemoryTrim", ex.Message);
                }
            };

            trimTimer.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _sessionController?.Dispose();
            if (_ownsMutex)
            {
                _singleInstanceMutex?.ReleaseMutex();
                _ownsMutex = false;
            }
            _singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
