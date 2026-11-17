using System.Windows;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Tests.Domain.Web
{
    /// <summary>
    /// Bitmiş ağlara ara sıra esen hafif rüzgârı doğrular: ne zaman başladığı, ne kadar sürdüğü, ne kadar güçlü olduğu
    /// ve hangi yöne estiği. Rastgele sayılar sabit tohumlarla (seed) üretilir; her test birkaç tohumla tekrarlanır.
    /// </summary>
    public class WebBreezeTests
    {
        private const double FrameSeconds = 0.02;
        private static readonly int[] Seeds = { 1, 7, 42, 2024, 31337 };

        private static double Offset(SpiderWebEntity web) => web.Breeze.Offset;
        private static Vector Direction(SpiderWebEntity web) => web.Breeze.Direction;

        private static SpiderWebEntity FinishedWeb() =>
            new(new Point(500, 500), new Point(500, 500), 100, new Queue<WebBuildStep>());

        private static SpiderWebEntity UnfinishedWeb() =>
            new(new Point(500, 500), new Point(500, 500), 100,
                new Queue<WebBuildStep>(new[] { new WebBuildStep(new Point(450, 500), new Point(550, 500), DrawSilk: true, IsAnchor: true) }));

        private sealed record Gust(double Start, double End, double Strongest, Vector Direction);

        // Ağı verilen süre boyunca çalıştırır ve her rüzgârın başladığı/bittiği anı kaydeder
        private static List<Gust> Run(SpiderWebEntity web, int seed, double seconds, Action<SpiderWebEntity>? eachFrame = null)
        {
            var rng = new Random(seed);
            var gusts = new List<Gust>();
            double start = -1, strongest = 0;
            for (double t = FrameSeconds; t <= seconds; t += FrameSeconds)
            {
                web.Update(FrameSeconds, rng);
                eachFrame?.Invoke(web);
                double offset = Offset(web);
                if (offset != 0)
                {
                    if (start < 0) start = t;
                    strongest = Math.Max(strongest, Math.Abs(offset));
                }
                else if (start >= 0)
                {
                    gusts.Add(new Gust(start, t, strongest, Direction(web)));
                    start = -1;
                    strongest = 0;
                }
            }
            return gusts;
        }

        [Fact]
        public void UnfinishedWeb_NeverSways()
        {
            foreach (int seed in Seeds)
            {
                Assert.Empty(Run(UnfinishedWeb(), seed, 200));
            }
        }

        [Fact]
        public void FirstGust_ComesAfter22Seconds()
        {
            foreach (int seed in Seeds)
            {
                Gust first = Run(FinishedWeb(), seed, 60)[0];
                Assert.InRange(first.Start, 22.0, 22.0 + 2 * FrameSeconds);
            }
        }

        [Fact]
        public void LaterGusts_Come22To50SecondsAfterThePreviousOne()
        {
            foreach (int seed in Seeds)
            {
                List<Gust> gusts = Run(FinishedWeb(), seed, 400);
                Assert.True(gusts.Count >= 5, $"400 saniyede yalnızca {gusts.Count} rüzgâr esti");
                for (int i = 1; i < gusts.Count; i++)
                {
                    Assert.InRange(gusts[i].Start - gusts[i - 1].End, 22.0 - FrameSeconds, 50.0 + 2 * FrameSeconds);
                }
            }
        }

        [Fact]
        public void EachGust_LastsAboutTwoToThreeSeconds_AndThenStops()
        {
            foreach (int seed in Seeds)
            {
                foreach (Gust gust in Run(FinishedWeb(), seed, 400))
                {
                    Assert.InRange(gust.End - gust.Start, 2.2 - 2 * FrameSeconds, 3.0 + 2 * FrameSeconds);
                }
            }
        }

        [Fact]
        public void Gusts_AreGentle_AtMostFiveAndAHalfPixels()
        {
            foreach (int seed in Seeds)
            {
                List<Gust> gusts = Run(FinishedWeb(), seed, 400);
                Assert.All(gusts, gust => Assert.InRange(gust.Strongest, 1.0, 5.5));
            }
        }

        [Fact]
        public void Gusts_BlowMostlySideways()
        {
            foreach (int seed in Seeds)
            {
                foreach (Gust gust in Run(FinishedWeb(), seed, 400))
                {
                    Assert.Equal(1.0, gust.Direction.Length, precision: 6);
                    Assert.True(Math.Abs(gust.Direction.X) > Math.Abs(gust.Direction.Y), $"Yön çok dik: {gust.Direction}");
                }
            }
        }

        [Fact]
        public void WhileAGustBlows_TheWebIsRedrawnEveryFrame()
        {
            bool sawStrongGust = false;
            Run(FinishedWeb(), seed: 42, seconds: 60, eachFrame: web =>
            {
                if (Math.Abs(Offset(web)) > 0.1)
                {
                    sawStrongGust = true;
                    Assert.True(web.IsDeforming);
                    Assert.True(web.IsAnimating);
                }
            });
            Assert.True(sawStrongGust);
        }
    }
}
