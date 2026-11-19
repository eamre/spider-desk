using System.Windows;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Tests.Domain.Spider
{
    /// <summary>
    /// "Örümcek" sekmesindeki seviye bilgisini doğrular: örümcek her 5 avda bir seviye atlar,
    /// ilerleme çubuğu ve "kaç av kaldı" yazısı buna göre değişir.
    /// </summary>
    public class SpiderStatusDisplayTests
    {
        [Fact]
        public void ANewSpider_IsLevelOne_WithAnEmptyBar()
        {
            SpiderStatusDisplay display = SpiderStatusDisplay.For(bugsCaught: 0, satietyPercent: 70, activity: "Gezinti");

            Assert.Equal(1, display.Level);
            Assert.Equal(0, display.LevelProgress);
            Assert.Equal("5 av sonra seviye 2", display.NextLevelText);
        }

        [Theory]
        [InlineData(3, 1, 0.6, "2 av sonra seviye 2")]
        [InlineData(4, 1, 0.8, "1 av sonra seviye 2")]
        [InlineData(5, 2, 0.0, "5 av sonra seviye 3")]
        [InlineData(13, 3, 0.6, "2 av sonra seviye 4")]
        public void EveryFiveBugs_RaiseTheLevel(int bugs, int level, double progress, string next)
        {
            SpiderStatusDisplay display = SpiderStatusDisplay.For(bugs, 50, "Avlanıyor");

            Assert.Equal(level, display.Level);
            Assert.Equal(progress, display.LevelProgress, precision: 6);
            Assert.Equal(next, display.NextLevelText);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(27)]
        public void TheLevel_IsTheSameAsTheSpidersOwn(int bugs)
        {
            var spider = new SpiderEntity(new Point(0, 0), new SpiderGaitController(), initialBugsCaught: bugs);

            Assert.Equal(spider.Level, SpiderStatusDisplay.For(bugs, 50, "").Level);
        }

        [Fact]
        public void SatietyBugsAndActivity_ArePassedThrough()
        {
            SpiderStatusDisplay display = SpiderStatusDisplay.For(13, 72, "Ağda Bekliyor");

            Assert.Equal(72, display.SatietyPercent);
            Assert.Equal(13, display.BugsCaught);
            Assert.Equal("Ağda Bekliyor", display.Activity);
        }
    }
}
