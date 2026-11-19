using System.Windows;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Spider.States;
using SpiderDesk.Domain.Web;
using SpiderDesk.Infrastructure.Sessions;

namespace SpiderDesk.Tests.Infrastructure.Sessions
{
    /// <summary>
    /// "Örümcek" sekmesindeki komutların örümceğe ulaştığını ve örümceğin durumunun sekmeye yalnızca değişince gönderildiğini doğrular.
    /// </summary>
    public class SpiderSessionControllerTests
    {
        private sealed class FakeView : ISpiderView
        {
            public event Action? NewWebRequested;
            public event Action? RappelRequested;
            public event Action? RainRequested;
            public event Action? SpawnBugsRequested;
            public event Action? ClearWebsRequested;

            public List<SpiderStatusDisplay> Shown { get; } = new();
            public void ShowStatus(SpiderStatusDisplay status) => Shown.Add(status);

            public void PressNewWeb() => NewWebRequested?.Invoke();
            public void PressRappel() => RappelRequested?.Invoke();
            public void PressRain() => RainRequested?.Invoke();
            public void PressSpawnBugs() => SpawnBugsRequested?.Invoke();
            public void PressClearWebs() => ClearWebsRequested?.Invoke();
        }

        private sealed class FakeEnvironment : IEnvironmentService
        {
            public EnvironmentTimeMode TimeMode { get; set; }
            public bool IsNightTime => false;
            public TimeSpan LocalTimeOfDay => TimeSpan.FromHours(10);
        }

        private readonly FakeView _view = new();
        private readonly SimulationEngine _engine;
        private readonly SpiderSessionController _controller;

        public SpiderSessionControllerTests()
        {
            _engine = new SimulationEngine(new BugSpeciesCatalog(), new FakeEnvironment(), new UserSettings { TotalBugsCaught = 13 }, 1920, 1080);
            _controller = new SpiderSessionController(_engine, _view);
        }

        [Fact]
        public void OnOpening_ShowsTheSpidersStatus()
        {
            SpiderStatusDisplay shown = _view.Shown.Single();

            Assert.Equal(3, shown.Level);
            Assert.Equal(13, shown.BugsCaught);
            Assert.Equal(_engine.Spider.CurrentState.DisplayName, shown.Activity);
        }

        [Fact]
        public void TheStatus_IsSentOnlyWhenSomethingChanges()
        {
            for (int i = 0; i < 100; i++) _controller.ShowStatus();
            Assert.Single(_view.Shown);

            _engine.Spider.RecordPreyCaught();
            _controller.ShowStatus();

            Assert.Equal(2, _view.Shown.Count);
            Assert.Equal(14, _view.Shown[^1].BugsCaught);
        }

        [Fact]
        public void NewWeb_SendsTheSpiderToSpinOne()
        {
            _view.PressNewWeb();

            Assert.IsType<SpinningWebState>(_engine.Spider.CurrentState);
            Assert.Single(_engine.World.Webs);
        }

        [Fact]
        public void Rappel_SendsTheSpiderUpToTheCeiling()
        {
            _engine.Spider.TransitionTo(new WanderingState(), _engine.World);

            _view.PressRappel();

            Assert.IsType<RappellingState>(_engine.Spider.CurrentState);
        }

        [Fact]
        public void SpawnBugs_SendsThreeBugs()
        {
            int before = _engine.World.Bugs.Count;

            _view.PressSpawnBugs();

            Assert.Equal(before + SpiderSessionController.BugsPerSpawn, _engine.World.Bugs.Count);
        }

        [Fact]
        public void ClearWebs_RemovesTheWebs_AndTheSpiderStartsANewOne()
        {
            _view.PressNewWeb();
            var oldWeb = _engine.World.Webs.Single();

            _view.PressClearWebs();

            Assert.DoesNotContain(oldWeb, _engine.World.Webs);
            Assert.IsType<SpinningWebState>(_engine.Spider.CurrentState);
        }

        [Fact]
        public void Rain_PutsDewDropsOnAFinishedWeb()
        {
            var steps = new Queue<WebBuildStep>(new[]
            {
                new WebBuildStep(new Point(400, 400), new Point(600, 400), DrawSilk: true, IsAnchor: false),
                new WebBuildStep(new Point(500, 300), new Point(500, 500), DrawSilk: true, IsAnchor: false)
            });
            var web = new SpiderWebEntity(new Point(500, 400), new Point(500, 400), 120, steps);
            while (web.TryBeginNextStep(out _)) web.CompleteCurrentStep();
            _engine.World.AddWeb(web);

            _view.PressRain();

            Assert.NotEmpty(web.Droplets.AttachedDroplets);
        }

        [Fact]
        public void Rain_WithoutAFinishedWeb_DoesNothingHarmful()
        {
            _view.PressRain();

            Assert.Empty(_engine.World.Webs);
        }
    }
}
