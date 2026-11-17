using System.Windows;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Spider.States;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Tests.Domain.Spider
{
    /// <summary>
    /// Örümceğin tavana çıkıp ipek iple sarktığı, sallandığı ve geri tırmandığı normal sarkmayı doğrular.
    /// Rastgele sayılar kullanıldığı için her test birkaç kez çalıştırılır ve sonuçların izin verilen aralıkta kaldığına bakılır.
    /// </summary>
    public class RappellingTests
    {
        private const double FrameSeconds = 0.02;
        private const int Runs = 15;

        private sealed record RappelTrace(List<(double Time, Point Position)> Points, ISpiderBehaviorState FinalState, double TotalSeconds);

        private static RappelTrace Rappel(Point start, Action<SpiderEntity, WorldContext, double>? eachFrame = null, SpiderWebEntity? home = null)
        {
            var world = new WorldContext();
            var spider = new SpiderEntity(start, new SpiderGaitController());
            if (home != null)
            {
                world.Webs.Add(home);
                // İkinci bitmiş ağ, otomatik ağ takvimindeki bir sonraki ağı ileri iter; örümcek hemen yeni ağa gitmez
                world.Webs.Add(new SpiderWebEntity(new Point(1500, 800), new Point(1500, 800), 100, new Queue<WebBuildStep>()));
                spider.ActiveWeb = home;
            }
            spider.TransitionTo(new RappellingState(), world);

            var points = new List<(double, Point)>();
            double t = 0;
            while (t < 60 && spider.CurrentState is RappellingState)
            {
                eachFrame?.Invoke(spider, world, t);
                spider.Update(world, FrameSeconds);
                t += FrameSeconds;
                points.Add((t, spider.Position));
            }
            return new RappelTrace(points, spider.CurrentState, t);
        }

        [Fact]
        public void FirstClimbsToTheTopOfTheScreen()
        {
            var trace = Rappel(new Point(900, 700));

            Assert.True(trace.Points.Min(p => p.Position.Y) <= 25, "Örümcek tavana çıkmadı");
        }

        [Fact]
        public void HangingPoint_StaysAwayFromTheScreenEdges()
        {
            for (int i = 0; i < Runs; i++)
            {
                var nearLeftEdge = Rappel(new Point(10, 700));
                double topX = nearLeftEdge.Points.First(p => p.Position.Y <= 25).Position.X;
                Assert.InRange(topX, 139.5, 1920 - 139.5);
            }
        }

        [Fact]
        public void HangingPoint_IsNearWhereTheSpiderWas()
        {
            for (int i = 0; i < Runs; i++)
            {
                var trace = Rappel(new Point(900, 700));
                double topX = trace.Points.First(p => p.Position.Y <= 25).Position.X;
                Assert.InRange(topX, 900 - 80.5, 900 + 80.5);
            }
        }

        [Fact]
        public void DropsBetween180PixelsAndAbout40PercentOfTheScreen()
        {
            // İp her karede biraz uzar; hedefe vardığı son karede hedefi en fazla bir karelik iniş kadar geçebilir
            const double oneFrameOfDescent = SpiderTuning.RappelDescendSpeed * FrameSeconds;
            for (int i = 0; i < Runs; i++)
            {
                var trace = Rappel(new Point(900, 50));
                double deepest = trace.Points.Max(p => p.Position.Y);
                Assert.InRange(deepest, 180 - 1, 180 + 1080 * 0.38 + oneFrameOfDescent);
            }
        }

        // Örümceğin en alt noktada ne kadar kaldığı: en derine vardığı andan yukarı tırmanmaya başladığı ana kadar
        private static double SecondsAtTheBottom(RappelTrace trace)
        {
            double deepest = trace.Points.Max(p => p.Position.Y);
            double reached = trace.Points.First(p => p.Position.Y >= deepest - 1).Time;
            double left = trace.Points.Last(p => p.Position.Y >= deepest - 1).Time;
            return left - reached;
        }

        [Fact]
        public void HangsForFiveToNineSeconds_BeforeClimbingBack()
        {
            for (int i = 0; i < Runs; i++)
            {
                Assert.InRange(SecondsAtTheBottom(Rappel(new Point(900, 50))), 5.0 - 0.1, 9.0 + 0.1);
            }
        }

        [Fact]
        public void WithoutAWeb_GoesWanderingAfterwards()
        {
            var trace = Rappel(new Point(900, 700));

            Assert.IsType<WanderingState>(trace.FinalState);
        }

        [Fact]
        public void WithAWeb_GoesBackToItsWebAfterwards()
        {
            var home = new SpiderWebEntity(new Point(400, 400), new Point(400, 400), 100, new Queue<WebBuildStep>());

            var trace = Rappel(new Point(900, 700), home: home);

            Assert.IsType<WaitingOnWebState>(trace.FinalState);
        }

        [Fact]
        public void FastCursorPushWhileHanging_SendsTheSpiderBackUpEarly()
        {
            for (int i = 0; i < Runs; i++)
            {
                double hangingSince = -1;
                double previousY = double.NaN;
                var pushed = Rappel(new Point(900, 50), (spider, world, t) =>
                {
                    // Örümcek aşağı inmeyi bitirince (artık alçalmıyorsa) yarım saniye bekle, sonra fareyle hızla üstüne git
                    if (hangingSince < 0 && spider.Position.Y > 170 && spider.Position.Y == previousY) hangingSince = t;
                    previousY = spider.Position.Y;
                    if (hangingSince >= 0 && t - hangingSince > 0.5)
                    {
                        world.CursorPosition = spider.Position;
                        world.CursorSpeed = SpiderTuning.RappelCursorPushSpeed * 2;
                    }
                });

                // İtilmeseydi en az 5 saniye kalırdı
                Assert.True(hangingSince >= 0, "Örümcek aşağıda hiç durmadı");
                Assert.InRange(SecondsAtTheBottom(pushed), 0.4, 1.5);
            }
        }

        [Fact]
        public void HangsHeadDown_AndClimbsBackHeadUp()
        {
            var world = new WorldContext();
            var spider = new SpiderEntity(new Point(900, 50), new SpiderGaitController());
            spider.TransitionTo(new RappellingState(), world);

            bool sawHeadDown = false, sawHeadUp = false;
            double previousY = spider.Position.Y;
            bool wentDown = false;
            for (double t = 0; t < 60 && spider.CurrentState is RappellingState; t += FrameSeconds)
            {
                spider.Update(world, FrameSeconds);
                bool isHanging = spider.Position.Y > 150 && spider.Position.Y == previousY;
                if (isHanging && Math.Abs(spider.Angle - Math.PI / 2) < 0.1) sawHeadDown = true;
                if (spider.Position.Y > previousY + 0.5) wentDown = true;
                if (wentDown && spider.Position.Y < previousY - 0.5 && Math.Abs(spider.Angle + Math.PI / 2) < 0.2) sawHeadUp = true;
                previousY = spider.Position.Y;
            }

            Assert.True(sawHeadDown, "Sarkarken baş aşağı durmadı");
            Assert.True(sawHeadUp, "Geri tırmanırken başı yukarı dönmedi");
        }
    }
}
