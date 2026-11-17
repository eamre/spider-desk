using System.Windows;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Domain.Insects.States;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Spider.States;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Tests.Domain.Spider
{
    /// <summary>
    /// Örümceğin hangi böceğin peşine düştüğünü ve hangi kilerden yediğini doğrular.
    /// Öncelik sırası: 1) hemen yanından geçen uçan böcek, 2) kendi ağına takılan böcek, 3) diğer ağlardaki en yakın böcek.
    /// </summary>
    public class PreySearchTests
    {
        private const double FrameSeconds = 0.02;
        private static readonly Point SpiderStart = new(500, 500);

        private readonly WorldContext _world = new();
        private readonly SpiderEntity _spider = new(SpiderStart, new SpiderGaitController());

        private BugEntity FlyingBugAt(double x, double y)
        {
            var bug = new BugEntity(new Point(x, y), new Vector(0, 0), 8, new HouseFlySpecies());
            _world.Bugs.Add(bug);
            return bug;
        }

        private BugEntity TrappedBugAt(double x, double y, SpiderWebEntity? web, double huntDelaySeconds = 0)
        {
            var bug = FlyingBugAt(x, y);
            bug.TransitionTo(new TrappedBugState(web, huntDelaySeconds), _world);
            return bug;
        }

        private SpiderWebEntity WebAt(double x, double y)
        {
            var web = new SpiderWebEntity(new Point(x, y), new Point(x, y), 100, new Queue<WebBuildStep>());
            _world.Webs.Add(web);
            return web;
        }

        // Örümceği bir kare ilerletip peşine düştüğü böceği verir (avlanmıyorsa null)
        private BugEntity? ChosenPrey()
        {
            _spider.Update(_world, FrameSeconds);
            return (_spider.CurrentState as HuntingState)?.Target;
        }

        [Fact]
        public void NoBugs_NoHunt()
        {
            Assert.Null(ChosenPrey());
        }

        [Fact]
        public void FlyingBugRightNextToTheSpider_IsHunted()
        {
            var bug = FlyingBugAt(SpiderStart.X + 30, SpiderStart.Y);

            Assert.Same(bug, ChosenPrey());
        }

        [Fact]
        public void FlyingBugFurtherAway_IsIgnored()
        {
            FlyingBugAt(SpiderStart.X + SpiderTuning.DesktopEncounterRadius + 5, SpiderStart.Y);

            Assert.Null(ChosenPrey());
        }

        [Fact]
        public void OfTwoNearbyFlyingBugs_TheCloserOneIsHunted()
        {
            FlyingBugAt(SpiderStart.X + 40, SpiderStart.Y);
            var closer = FlyingBugAt(SpiderStart.X, SpiderStart.Y + 20);

            Assert.Same(closer, ChosenPrey());
        }

        [Fact]
        public void NearbyFlyingBug_WinsOverABugTrappedInItsOwnWeb()
        {
            var home = WebAt(SpiderStart.X, SpiderStart.Y);
            _spider.ActiveWeb = home;
            TrappedBugAt(SpiderStart.X + 10, SpiderStart.Y, home);
            var flying = FlyingBugAt(SpiderStart.X + 30, SpiderStart.Y);

            Assert.Same(flying, ChosenPrey());
        }

        [Fact]
        public void BugInItsOwnWeb_WinsOverACloserBugInAnotherWeb()
        {
            var home = WebAt(SpiderStart.X + 400, SpiderStart.Y);
            var other = WebAt(SpiderStart.X + 100, SpiderStart.Y);
            _spider.ActiveWeb = home;
            TrappedBugAt(SpiderStart.X + 100, SpiderStart.Y, other);
            var inHome = TrappedBugAt(SpiderStart.X + 400, SpiderStart.Y, home);

            Assert.Same(inHome, ChosenPrey());
        }

        [Fact]
        public void BugInItsOwnWeb_IsHuntedEvenBeforeItsHuntDelayIsOver()
        {
            var home = WebAt(SpiderStart.X + 200, SpiderStart.Y);
            _spider.ActiveWeb = home;
            var inHome = TrappedBugAt(SpiderStart.X + 200, SpiderStart.Y, home, huntDelaySeconds: 10);

            Assert.Same(inHome, ChosenPrey());
        }

        [Fact]
        public void WithoutAHomeWeb_TheClosestReadyTrappedBugIsHunted()
        {
            var webA = WebAt(SpiderStart.X + 300, SpiderStart.Y);
            var webB = WebAt(SpiderStart.X - 150, SpiderStart.Y);
            TrappedBugAt(SpiderStart.X + 300, SpiderStart.Y, webA);
            var closer = TrappedBugAt(SpiderStart.X - 150, SpiderStart.Y, webB);

            Assert.Same(closer, ChosenPrey());
        }

        [Fact]
        public void TrappedBugInAnotherWeb_IsNotHuntedBeforeItsHuntDelayIsOver()
        {
            var web = WebAt(SpiderStart.X + 200, SpiderStart.Y);
            TrappedBugAt(SpiderStart.X + 200, SpiderStart.Y, web, huntDelaySeconds: 10);

            Assert.Null(ChosenPrey());
        }

        [Fact]
        public void Pantry_ItsOwnWebIsPreferred_EvenIfAnotherIsCloser()
        {
            var home = WebAt(SpiderStart.X + 400, SpiderStart.Y);
            var other = WebAt(SpiderStart.X + 50, SpiderStart.Y);
            home.Pantry.StoreCocoon(home.Center, 6, 0);
            other.Pantry.StoreCocoon(other.Center, 6, 0);
            _spider.ActiveWeb = home;

            Assert.Same(home, PantrySearch());
        }

        [Fact]
        public void Pantry_WithoutFoodAtHome_TheClosestWebWithFoodIsChosen()
        {
            var home = WebAt(SpiderStart.X, SpiderStart.Y);
            var far = WebAt(SpiderStart.X + 400, SpiderStart.Y);
            var near = WebAt(SpiderStart.X - 100, SpiderStart.Y);
            far.Pantry.StoreCocoon(far.Center, 6, 0);
            near.Pantry.StoreCocoon(near.Center, 6, 0);
            _spider.ActiveWeb = home;

            Assert.Same(near, PantrySearch());
        }

        [Fact]
        public void Pantry_WhenNoWebHasFood_GivesNothing()
        {
            WebAt(SpiderStart.X, SpiderStart.Y);

            Assert.Null(PantrySearch());
        }

        private SpiderWebEntity? PantrySearch() => PreySearch.FindWebWithStoredFood(_spider, _world);
    }
}
