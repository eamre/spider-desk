using System;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain;
using SpiderDesk.Infrastructure.Platform;
using SpiderDesk.Infrastructure.Windowing;
using SpiderDesk.Rendering;
using SpiderDesk.Rendering.Web;

namespace SpiderDesk.Infrastructure.Sessions
{
    /// <summary>
    /// Ayarlar sekmesini (<see cref="ISettingsView"/>) simülasyona ve ayar dosyasına bağlar.
    /// Değişen her ayarı önce uygular, sonra <see cref="SettingsStore"/> ile kaydeder; kayıt başarısız olursa görünüm uyarı gösterir.
    /// Örümceğin toplam av sayısı da ayarlarla birlikte kaydedilir.
    /// </summary>
    public sealed class SettingsSessionController
    {
        private readonly ISettingsView _view;
        private readonly SimulationEngine _engine;
        private readonly SettingsStore _settings;
        private readonly SimulationCanvas _canvas;
        private readonly WebRenderer _webRenderer;
        private readonly IWindowLayerService _windowLayerService;
        private readonly IStartupRegistrationService _startupService;

        public SettingsSessionController(
            ISettingsView view,
            SimulationEngine engine,
            SettingsStore settings,
            SimulationCanvas canvas,
            WebRenderer webRenderer,
            IWindowLayerService windowLayerService,
            IStartupRegistrationService startupService)
        {
            _view = view;
            _engine = engine;
            _settings = settings;
            _canvas = canvas;
            _webRenderer = webRenderer;
            _windowLayerService = windowLayerService;
            _startupService = startupService;

            _view.ShowSettings(settings.Current, startupService.IsEnabled());

            _view.ThemeSelected += theme =>
            {
                _engine.SetTheme(theme);
                _webRenderer.ApplyTheme(theme);
                _canvas.RefreshLayers(forceAll: true);
                Save(s => s.ThemeId = theme.Id);
            };

            _view.SpawnRateSelected += rate =>
            {
                _engine.SetSpawnRate(rate);
                Save(s => s.SpawnRate = rate);
            };

            _view.DewSpawnRateSelected += rate =>
            {
                _engine.SetDewSpawnRate(rate);
                Save(s => s.DewSpawnRate = rate);
            };

            _view.SpeedPresetSelected += preset =>
            {
                _engine.SetSpeedPreset(preset);
                Save(s => s.SpeedPreset = preset);
            };

            _view.TimeModeSelected += mode =>
            {
                _engine.SetTimeMode(mode);
                Save(s => s.TimeMode = mode);
            };

            _view.AlwaysOnTopChanged += alwaysOnTop =>
            {
                _windowLayerService.AlwaysOnTop = alwaysOnTop;
                _windowLayerService.EnforceZOrder();
                Save(s => s.AlwaysOnTop = alwaysOnTop);
            };

            _view.StartupChanged += enabled =>
            {
                _startupService.SetEnabled(enabled);
                Save(s => s.RunOnStartup = enabled);
            };

            _engine.Spider.PreyCaught += totalCaught => Save(s => s.TotalBugsCaught = totalCaught);
        }

        private void Save(Action<UserSettings> change) => _view.ShowSaveFailed(!_settings.Update(change));
    }
}
