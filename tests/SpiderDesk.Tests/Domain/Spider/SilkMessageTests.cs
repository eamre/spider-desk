using System.Collections.Generic;
using System.Windows;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Spider.States;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Tests.Domain.Spider
{
    /// <summary>
    /// Örümceğe ipekle yazması için verilen mesajların ne zaman yazıldığını ve yazıdan sonra nereye gittiğini doğrular.
    /// </summary>
    public class SilkMessageTests
    {
        private const double FrameSeconds = 0.02;

        private static (SpiderEntity Spider, WorldContext World) CreateWorld()
        {
            var world = new WorldContext();
            var spider = new SpiderEntity(new Point(1400, 350), new SpiderGaitController());
            return (spider, world);
        }

        private static SpiderWebEntity AddFinishedWeb(WorldContext world)
        {
            var web = new SpiderWebEntity(new Point(400, 400), new Point(400, 400), 100, new Queue<WebBuildStep>());
            world.Webs.Add(web);
            return web;
        }

        // Örümcek yazmayı bitirip başka bir duruma geçene kadar simülasyonu ilerletir
        private static ISpiderBehaviorState RunUntilWritingEnds(SpiderEntity spider, WorldContext world, double maxSeconds = 120)
        {
            for (double t = 0; t < maxSeconds && spider.CurrentState is WritingSilkMessageState; t += FrameSeconds)
            {
                spider.Update(world, FrameSeconds);
            }
            return spider.CurrentState;
        }

        [Fact]
        public void Message_WhileWandering_IsWrittenRightAway()
        {
            var (spider, world) = CreateWorld();

            spider.RequestSilkMessage("FOCUS");
            spider.Update(world, FrameSeconds);

            var writing = Assert.IsType<WritingSilkMessageState>(spider.CurrentState);
            Assert.Equal("FOCUS", writing.Text);
            Assert.False(spider.HasPendingSilkMessage);
            Assert.NotNull(world.SilkMessage);
        }

        [Fact]
        public void Message_WhileSpinningAWeb_WaitsUntilTheSpiderIsFree()
        {
            var (spider, world) = CreateWorld();
            spider.TransitionTo(SpinningWebState.CreateForBestSite(world), world);

            spider.RequestSilkMessage("FOCUS");
            spider.Update(world, FrameSeconds);

            Assert.IsType<SpinningWebState>(spider.CurrentState);
            Assert.True(spider.HasPendingSilkMessage);
        }

        [Fact]
        public void NewerMessage_ReplacesAnUnwrittenOne()
        {
            var (spider, world) = CreateWorld();
            spider.TransitionTo(SpinningWebState.CreateForBestSite(world), world);

            spider.RequestSilkMessage("FOCUS");
            spider.RequestSilkMessage("BREAK");
            spider.TransitionTo(new WanderingState(), world);
            spider.Update(world, FrameSeconds);

            var writing = Assert.IsType<WritingSilkMessageState>(spider.CurrentState);
            Assert.Equal("BREAK", writing.Text);
        }

        [Fact]
        public void Announcement_WhenFinished_ReturnsToItsWeb()
        {
            var (spider, world) = CreateWorld();
            var web = AddFinishedWeb(world);
            // İkinci ağ, otomatik ağ takvimindeki bir sonraki ağı 5 dakika sonraya iter; böylece örümcek yazıdan sonra
            // hemen yeni ağ örmeye gitmez ve testte yalnızca "ağına döner" kuralını görürüz
            AddFinishedWeb(world);
            spider.ActiveWeb = web;

            spider.TransitionTo(WritingSilkMessageState.Announcement("BREAK"), world);

            Assert.IsType<WaitingOnWebState>(RunUntilWritingEnds(spider, world));
            Assert.Same(web, spider.ActiveWeb);
        }

        [Fact]
        public void Announcement_WithoutAWeb_GoesWandering()
        {
            var (spider, world) = CreateWorld();

            spider.TransitionTo(WritingSilkMessageState.Announcement("FOCUS"), world);

            Assert.IsType<WanderingState>(RunUntilWritingEnds(spider, world));
        }

        [Fact]
        public void Greeting_WhenFinished_StartsSpinningTheFirstWeb()
        {
            var (spider, world) = CreateWorld();

            spider.TransitionTo(WritingSilkMessageState.Greeting("HELLO"), world);

            Assert.IsType<SpinningWebState>(RunUntilWritingEnds(spider, world));
        }
    }
}
