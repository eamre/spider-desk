using System;
using System.Windows;
using SpiderDesk.Core;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Domain.Insects.States;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Spider.States;
using SpiderDesk.Domain.Spider.Themes;

namespace SpiderDesk.Domain
{
    /// <summary>
    /// Simülasyon döngüsünü, ortam zamanını (IEnvironmentService) ve kalıcı kullanıcı ayarlarını koordine eder (SRP & DIP).
    /// </summary>
    public class SimulationEngine
    {
        private readonly BugSpawner _bugSpawner;
        private readonly IEnvironmentService _environmentService;
        private readonly ISettingsRepository _settingsRepository;
        private readonly UserSettings _settings;

        public WorldContext World { get; }
        public SpiderEntity Spider { get; }
        public UserSettings Settings => _settings;

        /// <summary>
        /// Ekranda hareket eden bir nesne (örümcek, böcek, titreşen ağ veya düşen damla) olup olmadığını bildirir.
        /// </summary>
        public bool HasVisualActivity
        {
            get
            {
                if (Spider.HasVisualMotion || World.Bugs.Count > 0)
                {
                    return true;
                }

                for (int i = 0; i < World.Webs.Count; i++)
                {
                    var web = World.Webs[i];
                    if (web.VibrationAmplitude > 0.05 || Math.Abs(web.BreezeOffset) > 0.05 || web.Droplets.FallingDroplets.Count > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public int GetStaticSceneRevision()
        {
            int rev = Spider.BugsCaught * 397;
            for (int i = 0; i < World.Webs.Count; i++)
            {
                var web = World.Webs[i];
                rev = (rev * 31) + web.CompletedSegments.Count + (web.Droplets.Revision * 17) + (web.DamagedSegments.Count * 7);
            }
            return rev;
        }

        public SimulationEngine(
            IBugSpeciesProvider speciesProvider,
            IEnvironmentService environmentService,
            ISettingsRepository settingsRepository)
        {
            _environmentService = environmentService;
            _settingsRepository = settingsRepository;
            _settings = _settingsRepository.Load();

            _environmentService.TimeMode = _settings.TimeMode;

            World = new WorldContext
            {
                ScreenWidth = SystemParameters.PrimaryScreenWidth,
                ScreenHeight = SystemParameters.PrimaryScreenHeight,
                IsNightTime = _environmentService.IsNightTime,
                DewSpawnRate = _settings.DewSpawnRate
            };

            _bugSpawner = new BugSpawner(speciesProvider, _settings.SpawnRate);
            var gaitController = new SpiderGaitController();

            Spider = new SpiderEntity(
                new Point(World.ScreenWidth * 0.72, World.ScreenHeight * 0.32),
                gaitController,
                initialBugsCaught: _settings.TotalBugsCaught
            )
            {
                Theme = SpiderThemeCatalog.GetById(_settings.ThemeId),
                SpeedMultiplier = _settings.SpeedMultiplier > 0.1 ? _settings.SpeedMultiplier : 1.0
            };

            Spider.PreyCaught += totalCaught =>
            {
                _settings.TotalBugsCaught = totalCaught;
                _settingsRepository.Save(_settings);
            };

            Spider.TransitionTo(SpinningWebState.CreateForBestSite(World), World);
        }

        public void Update(double dt, double screenWidth, double screenHeight, Point cursorPos, Point prevCursorPos, double cursorSpeed)
        {
            World.ScreenWidth = screenWidth;
            World.ScreenHeight = screenHeight;
            World.CursorPosition = cursorPos;
            World.PreviousCursorPosition = prevCursorPos;
            World.CursorSpeed = cursorSpeed;
            World.IsNightTime = _environmentService.IsNightTime;

            _bugSpawner.Update(World, dt);

            foreach (var web in World.Webs)
            {
                web.CheckCursorPluck(cursorPos, cursorSpeed);
                web.Update(dt, World.Random);
            }

            for (int i = World.Bugs.Count - 1; i >= 0; i--)
            {
                World.Bugs[i].Update(World, dt);
                if (World.Bugs[i].IsEaten)
                {
                    World.Bugs.RemoveAt(i);
                }
            }

            Spider.Update(World, dt);
        }

        public void SetTheme(ISpiderTheme theme)
        {
            Spider.Theme = theme;
            _settings.ThemeId = theme.Id;
            _settingsRepository.Save(_settings);
        }

        public void SetSpawnRate(BugSpawnRatePreset rate)
        {
            _bugSpawner.SpawnRate = rate;
            _settings.SpawnRate = rate;
            _settingsRepository.Save(_settings);
        }

        public void SetDewSpawnRate(DewSpawnRatePreset rate)
        {
            World.DewSpawnRate = rate;
            foreach (var web in World.Webs)
            {
                web.Droplets.SpawnRate = rate;
            }
            _settings.DewSpawnRate = rate;
            _settingsRepository.Save(_settings);
        }

        public void SetSpeedMultiplier(double multiplier)
        {
            Spider.SpeedMultiplier = multiplier;
            _settings.SpeedMultiplier = multiplier;
            _settingsRepository.Save(_settings);
        }

        public void SetTimeMode(EnvironmentTimeMode mode)
        {
            _environmentService.TimeMode = mode;
            _settings.TimeMode = mode;
            _settingsRepository.Save(_settings);
        }

        public void SetAlwaysOnTopPreference(bool alwaysOnTop)
        {
            _settings.AlwaysOnTop = alwaysOnTop;
            _settingsRepository.Save(_settings);
        }

        public void SetRunOnStartupPreference(bool runOnStartup)
        {
            _settings.RunOnStartup = runOnStartup;
            _settingsRepository.Save(_settings);
        }

        public void CommandNewWeb() => Spider.RequestNewWeb(World);

        public void CommandRappel() => Spider.RequestRappel(World);

        public void CommandRainOnWebs()
        {
            foreach (var web in World.Webs)
            {
                if (web.IsCompleted)
                {
                    web.Droplets.AddRainBurst(web.CompletedSegments, World.Random, count: 6);
                }
            }
        }

        public void SpawnExtraBugs(int count) => _bugSpawner.SpawnBurst(World, count);

        public void ClearWebs()
        {
            // Ağda asılı kalmış veya sarılmakta olan böcekleri serbest bırak (boşlukta asılı kalmalarını önler)
            for (int i = 0; i < World.Bugs.Count; i++)
            {
                var bug = World.Bugs[i];
                if (bug.CurrentState.Status == BugLifecycleStatus.TrappedInWeb || bug.CurrentState.Status == BugLifecycleStatus.BeingWrapped)
                {
                    bug.TrappedWeb = null;
                    bug.WrapProgress = 0.0;
                    double angle = World.Random.NextDouble() * Math.PI * 2;
                    Vector vel = new Vector(Math.Cos(angle), Math.Sin(angle)) * bug.Species.MinSpeed;
                    bug.SetMotion(bug.Position, vel, angle);
                    bug.TransitionTo(new FlyingBugState(webImmunitySeconds: 4.0), World);
                }
            }

            World.Webs.Clear();
            Spider.ClearPendingWebRequests();
            Spider.ActiveWeb = null;
            Spider.TransitionTo(SpinningWebState.CreateForBestSite(World), World);
        }
    }
}
