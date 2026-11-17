using System.Windows;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Domain.Insects.States;
using SpiderDesk.Domain.Spider.States;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Tests.Domain.Web
{
    /// <summary>
    /// Bir ağ ortadan kalkınca (Ağları Temizle, en eski ağın silinmesi ya da ağın kaybolması) ona takılı böceklerin
    /// serbest kalıp uçtuğunu ve ekranda en fazla 6 ağ kaldığını doğrular.
    /// </summary>
    public class ReleasingBugsTests
    {
        private sealed class FakeEnvironment : IEnvironmentService
        {
            public EnvironmentTimeMode TimeMode { get; set; }
            public bool IsNightTime => false;
            public TimeSpan LocalTimeOfDay => TimeSpan.FromHours(10);
        }

        private static SpiderWebEntity CompletedWebAt(double x, double y) =>
            new(new Point(x, y), new Point(x, y), 100, new Queue<WebBuildStep>());

        private static BugEntity TrappedBug(WorldContext world, SpiderWebEntity web, Point at)
        {
            var bug = new BugEntity(at, new Vector(0, 0), 8, new HouseFlySpecies());
            world.Bugs.Add(bug);
            bug.TransitionTo(new TrappedBugState(web, huntDelaySeconds: 0), world);
            return bug;
        }

        private static BugEntity BugBeingWrapped(WorldContext world, SpiderWebEntity web, Point at)
        {
            BugEntity bug = TrappedBug(world, web, at);
            bug.TransitionTo(new BeingWrappedBugState(), world);
            bug.WrapProgress = 0.5;
            return bug;
        }

        private static void AssertFlyingFree(BugEntity bug, Point stayedAt)
        {
            Assert.IsType<FlyingBugState>(bug.CurrentState);
            Assert.Null(bug.TrappedWeb);
            Assert.Equal(0.0, bug.WrapProgress);
            Assert.Equal(stayedAt, bug.Position);
            Assert.Equal(bug.Species.MinSpeed, bug.Velocity.Length, precision: 6);
        }

        [Fact]
        public void ClearingWebs_FreesTheBugsStuckInThem()
        {
            var engine = new SimulationEngine(new BugSpeciesCatalog(), new FakeEnvironment(), new UserSettings(), 1920, 1080);
            SpiderWebEntity web = CompletedWebAt(600, 500);
            engine.World.Webs.Add(web);
            var at = new Point(620, 510);
            BugEntity bug = TrappedBug(engine.World, web, at);

            engine.ClearWebs();

            AssertFlyingFree(bug, at);
            Assert.DoesNotContain(web, engine.World.Webs);
        }

        [Fact]
        public void ClearingWebs_FreesABugHalfWrapped_AndDropsItsSilk()
        {
            var engine = new SimulationEngine(new BugSpeciesCatalog(), new FakeEnvironment(), new UserSettings(), 1920, 1080);
            SpiderWebEntity web = CompletedWebAt(600, 500);
            engine.World.Webs.Add(web);
            var at = new Point(620, 510);
            BugEntity bug = BugBeingWrapped(engine.World, web, at);

            engine.ClearWebs();

            AssertFlyingFree(bug, at);
        }

        [Fact]
        public void ClearingWebs_LeavesFlyingBugsAlone()
        {
            var engine = new SimulationEngine(new BugSpeciesCatalog(), new FakeEnvironment(), new UserSettings(), 1920, 1080);
            var flying = new BugEntity(new Point(300, 300), new Vector(40, 0), 8, new HouseFlySpecies());
            engine.World.Bugs.Add(flying);
            IBugBehaviorState stateBefore = flying.CurrentState;

            engine.ClearWebs();

            Assert.Same(stateBefore, flying.CurrentState);
            Assert.Equal(new Vector(40, 0), flying.Velocity);
        }

        [Fact]
        public void WhenItsWebIsGone_ATrappedBugFreesItself()
        {
            var world = new WorldContext();
            SpiderWebEntity web = CompletedWebAt(600, 500);
            world.Webs.Add(web);
            var at = new Point(620, 510);
            BugEntity bug = TrappedBug(world, web, at);

            world.Webs.Remove(web);
            bug.Update(world, 0.02);

            AssertFlyingFree(bug, at);
        }

        private static WorldContext WorldWithSixWebs()
        {
            var world = new WorldContext();
            for (int i = 0; i < 6; i++)
            {
                SpinningWebState.CreateForBestSite(world);
            }
            Assert.Equal(6, world.Webs.Count);
            return world;
        }

        [Fact]
        public void TheSeventhWeb_ReplacesTheOldestOne()
        {
            WorldContext world = WorldWithSixWebs();
            SpiderWebEntity oldest = world.Webs[0];
            List<SpiderWebEntity> others = world.Webs.Skip(1).ToList();

            SpinningWebState.CreateForBestSite(world);

            Assert.Equal(6, world.Webs.Count);
            Assert.DoesNotContain(oldest, world.Webs);
            Assert.Equal(others, world.Webs.Take(5));
        }

        [Fact]
        public void TheSeventhWeb_FreesOnlyTheBugsOfTheOldestWeb()
        {
            WorldContext world = WorldWithSixWebs();
            var inOldest = new Point(world.Webs[0].Center.X + 5, world.Webs[0].Center.Y + 5);
            BugEntity bugInOldest = TrappedBug(world, world.Webs[0], inOldest);
            BugEntity bugInSecond = TrappedBug(world, world.Webs[1], world.Webs[1].Center);
            SpiderWebEntity second = world.Webs[1];

            SpinningWebState.CreateForBestSite(world);

            AssertFlyingFree(bugInOldest, inOldest);
            Assert.IsType<TrappedBugState>(bugInSecond.CurrentState);
            Assert.Same(second, bugInSecond.TrappedWeb);
        }

        [Fact]
        public void TheSeventhWeb_DropsTheSilkOfABugHalfWrappedOnTheOldestWeb()
        {
            WorldContext world = WorldWithSixWebs();
            var at = new Point(world.Webs[0].Center.X + 5, world.Webs[0].Center.Y + 5);
            BugEntity bug = BugBeingWrapped(world, world.Webs[0], at);

            SpinningWebState.CreateForBestSite(world);

            // Yarım kalan koza ipi böcekle birlikte uçmamalı
            AssertFlyingFree(bug, at);
        }
    }
}
