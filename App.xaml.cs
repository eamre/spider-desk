using System.Threading;
using System.Windows;
using SpiderDesk.Core;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Infrastructure;
using SpiderDesk.Rendering;

namespace SpiderDesk
{
    /// <summary>
    /// Uygulamanın Composition Root (Bağımlılık Enjeksiyonu Başlangıç Noktası) sınıfı (DIP).
    /// </summary>
    public partial class App : Application
    {
        private Mutex? _singleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            _singleInstanceMutex = new Mutex(true, "SpiderDesk_SingleInstance_Mutex", out bool isNewInstance);
            if (!isNewInstance)
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

            if (!startupService.IsEnabled())
            {
                startupService.SetEnabled(true);
            }

            // Domain ve Görselleştirme bileşenlerinin oluşturulması
            IBugSpeciesProvider speciesProvider = new BugSpeciesCatalog();
            var engine = new SimulationEngine(speciesProvider, environmentService, settingsRepository);

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

            var mainWindow = new MainWindow(
                engine,
                canvas,
                webRenderer,
                windowLayerService,
                cursorService,
                startupService,
                trayMenuService
            );
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
