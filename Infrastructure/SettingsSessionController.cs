using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain;
using SpiderDesk.Infrastructure.Tray;
using SpiderDesk.Rendering;
using SpiderDesk.Rendering.Web;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Tepsideki ayar menüsünü simülasyona ve ayar dosyasına bağlar.
    /// Menüden değişen her ayarı önce uygular, sonra <see cref="SettingsStore"/> ile kaydeder.
    /// Örümceğin toplam av sayısı da ayarlarla birlikte kaydedilir.
    /// </summary>
    public sealed class SettingsSessionController
    {
        private readonly SimulationEngine _engine;
        private readonly SettingsStore _settings;
        private readonly SimulationCanvas _canvas;
        private readonly WebRenderer _webRenderer;
        private readonly IWindowLayerService _windowLayerService;
        private readonly IStartupRegistrationService _startupService;

        public SettingsSessionController(
            SettingsMenu menu,
            SimulationEngine engine,
            SettingsStore settings,
            SimulationCanvas canvas,
            WebRenderer webRenderer,
            IWindowLayerService windowLayerService,
            IStartupRegistrationService startupService)
        {
            _engine = engine;
            _settings = settings;
            _canvas = canvas;
            _webRenderer = webRenderer;
            _windowLayerService = windowLayerService;
            _startupService = startupService;

            menu.ThemeSelected += theme =>
            {
                _engine.SetTheme(theme);
                _webRenderer.ApplyTheme(theme);
                _canvas.RefreshLayers(forceAll: true);
                _settings.Update(s => s.ThemeId = theme.Id);
            };

            menu.SpawnRateSelected += rate =>
            {
                _engine.SetSpawnRate(rate);
                _settings.Update(s => s.SpawnRate = rate);
            };

            menu.DewSpawnRateSelected += rate =>
            {
                _engine.SetDewSpawnRate(rate);
                _settings.Update(s => s.DewSpawnRate = rate);
            };

            menu.SpeedPresetSelected += preset =>
            {
                _engine.SetSpeedPreset(preset);
                _settings.Update(s => s.SpeedPreset = preset);
            };

            menu.TimeModeSelected += mode =>
            {
                _engine.SetTimeMode(mode);
                _settings.Update(s => s.TimeMode = mode);
            };

            menu.AlwaysOnTopChanged += alwaysOnTop =>
            {
                _windowLayerService.AlwaysOnTop = alwaysOnTop;
                _windowLayerService.EnforceZOrder();
                _settings.Update(s => s.AlwaysOnTop = alwaysOnTop);
            };

            menu.StartupChanged += enabled =>
            {
                _startupService.SetEnabled(enabled);
                _settings.Update(s => s.RunOnStartup = enabled);
            };

            _engine.Spider.PreyCaught += totalCaught => _settings.Update(s => s.TotalBugsCaught = totalCaught);
        }
    }
}
