using System;
using System.Windows;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Domain.Insects.States;
using SpiderDesk.Domain.SilkText;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Spider.States;
using SpiderDesk.Domain.Spider.Themes;

namespace SpiderDesk.Domain
{
    /// <summary>
    /// Simülasyon döngüsünü ve ortam zamanını (IEnvironmentService) koordine eder.
    /// Ayarları yalnızca uygular; kaydetmek SettingsStore'un işidir.
    /// </summary>
    public class SimulationEngine
    {
        private readonly BugSpawner _bugSpawner;
        private readonly IEnvironmentService _environmentService;

        public WorldContext World { get; }
        public SpiderEntity Spider { get; }

        /// <summary>
        /// Ekranda hareket eden bir nesne (örümcek, böcek, titreşen ağ veya düşen damla) olup olmadığını bildirir.
        /// </summary>
        public bool HasVisualActivity
        {
            get
            {
                if (Spider.HasVisualMotion || World.Bugs.Count > 0 || World.SilkMessage != null)
                {
                    return true;
                }

                for (int i = 0; i < World.Webs.Count; i++)
                {
                    if (World.Webs[i].IsAnimating)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public SimulationEngine(
            IBugSpeciesProvider speciesProvider,
            IEnvironmentService environmentService,
            UserSettings initialSettings,
            double screenWidth,
            double screenHeight)
        {
            _environmentService = environmentService;
            _environmentService.TimeMode = initialSettings.TimeMode;

            World = new WorldContext
            {
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight,
                IsNightTime = _environmentService.IsNightTime,
                DewSpawnRate = initialSettings.DewSpawnRate
            };

            _bugSpawner = new BugSpawner(speciesProvider, initialSettings.SpawnRate);
            var gaitController = new SpiderGaitController();

            Spider = new SpiderEntity(
                new Point(World.ScreenWidth * 0.72, World.ScreenHeight * 0.32),
                gaitController,
                initialBugsCaught: initialSettings.TotalBugsCaught
            )
            {
                Theme = SpiderThemeCatalog.GetById(initialSettings.ThemeId),
                SpeedMultiplier = SpiderSpeedPolicy.MultiplierFor(initialSettings.SpeedPreset)
            };

            // Açılışta, daha hiç ağ örmeden önce saate uygun selamı ipekle yazar; ardından ilk ağını örmeye gider.
            string greeting = GreetingPolicy.ChooseGreeting(_environmentService.LocalTimeOfDay);
            Spider.TransitionTo(WritingSilkMessageState.Greeting(greeting), World);
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
                web.CheckCursorPluck(cursorPos, cursorSpeed, World.Random);
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
            UpdateSilkMessage(dt);
        }

        private void UpdateSilkMessage(double dt)
        {
            if (World.SilkMessage is not { } greeting) return;

            greeting.Update(dt);
            if (greeting.IsFinished)
            {
                World.SilkMessage = null;
            }
        }

        public void SetTheme(ISpiderTheme theme)
        {
            Spider.Theme = theme;
        }

        public void SetSpawnRate(BugSpawnRatePreset rate)
        {
            _bugSpawner.SpawnRate = rate;
        }

        public void SetDewSpawnRate(DewSpawnRatePreset rate)
        {
            World.DewSpawnRate = rate;
            foreach (var web in World.Webs)
            {
                web.Droplets.SetSpawnRate(rate, World.Random);
            }
        }

        public void SetSpeedPreset(SpiderSpeedPreset preset)
        {
            Spider.SpeedMultiplier = SpiderSpeedPolicy.MultiplierFor(preset);
        }

        public void SetTimeMode(EnvironmentTimeMode mode)
        {
            _environmentService.TimeMode = mode;
        }

        public void CommandNewWeb() => Spider.RequestNewWeb(World);

        /// <summary>Örümcek boşalır boşalmaz bu mesajı ipekle yazar (örneğin Pomodoro'nun FOCUS / BREAK duyuruları).</summary>
        public void RequestSilkMessage(string text) => Spider.RequestSilkMessage(text);

        /// <summary>Örümceğin henüz yazmaya başlamadığı mesajı iptal eder (örneğin Pomodoro durdurulduğunda).</summary>
        public void CancelPendingSilkMessage() => Spider.CancelPendingSilkMessage();

        /// <summary>
        /// Örümceğin sırada bekleyen ya da şu an yazdığı bir duyurusu varsa true döner.
        /// Pomodoro, yeni turun süresini örümcek duyuruyu yazıp bitirince başlatmak için buna bakar.
        /// </summary>
        public bool IsBusyWithAnnouncement =>
            Spider.HasPendingSilkMessage || Spider.CurrentState is WritingSilkMessageState { IsAnnouncement: true };

        /// <summary>
        /// Örümcek şu an bir duyuru yazıyorsa, yazdığı duyuru hâlâ soluyorsa ya da tavandan bir not gösteriyorsa true döner.
        /// Bu sürede örümcek ve yazısı/notu, kullanıcı tam ekran bir uygulamadayken bile görünmelidir.
        /// </summary>
        public bool IsShowingAnnouncement =>
            World.SilkMessage is { IsAnnouncement: true } || Spider.CarriedNote != null;

        public void CommandRappel() => Spider.RequestRappel(World);

        /// <summary>
        /// Örümcek elindeki işi bitirince tavandan sarkıp bu notu ön bacaklarıyla gösterir (örneğin bir hatırlatma).
        /// Birden fazla not gelirse sırayla gösterilir.
        /// </summary>
        public void RequestNote(string text) => Spider.RequestNote(text);

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
