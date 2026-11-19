using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Infrastructure.Flyout.Pages;

namespace SpiderDesk.Tests.Infrastructure.Flyout.Pages
{
    /// <summary>
    /// "Örümcek" sekmesinin kendisini doğrular: durumu doğru gösterdiğini ve her komut düğmesinin kendi isteğini bildirdiğini.
    /// </summary>
    public class SpiderPageTests
    {
        private static T Find<T>(SpiderPage page, string name) where T : class => (T)page.FindName(name);

        private static void Press(Button button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        [Fact]
        public void ShowsTheLevel_TheProgress_AndTheStats()
        {
            StaThread.Run(() =>
            {
                var page = new SpiderPage();

                page.ShowStatus(SpiderStatusDisplay.For(bugsCaught: 13, satietyPercent: 72, activity: "Ağda Bekliyor"));

                Assert.Equal("3", Find<TextBlock>(page, "LevelBadgeText").Text);
                Assert.Equal("Seviye 3", Find<TextBlock>(page, "LevelText").Text);
                Assert.Equal("2 av sonra seviye 4", Find<TextBlock>(page, "NextLevelText").Text);
                Assert.Equal(0.6, Find<ScaleTransform>(page, "LevelFillScale").ScaleX, precision: 6);
                Assert.Equal("%72", Find<TextBlock>(page, "SatietyText").Text);
                Assert.Equal("13", Find<TextBlock>(page, "BugsText").Text);
                Assert.Equal("Ağda Bekliyor", Find<TextBlock>(page, "ActivityText").Text);
            });
        }

        [Fact]
        public void TheSpawnButton_SaysHowManyBugsItSends()
        {
            StaThread.Run(() =>
            {
                Assert.Equal("3 böcek gönder", Find<TextBlock>(new SpiderPage(), "SpawnBugsText").Text);
            });
        }

        [Theory]
        [InlineData("NewWebButton", "newWeb")]
        [InlineData("RappelButton", "rappel")]
        [InlineData("RainButton", "rain")]
        [InlineData("SpawnBugsButton", "bugs")]
        [InlineData("ClearWebsButton", "clear")]
        public void EachCommandButton_AsksForItsOwnCommand(string buttonName, string expected)
        {
            StaThread.Run(() =>
            {
                var page = new SpiderPage();
                var asked = new List<string>();
                page.NewWebRequested += () => asked.Add("newWeb");
                page.RappelRequested += () => asked.Add("rappel");
                page.RainRequested += () => asked.Add("rain");
                page.SpawnBugsRequested += () => asked.Add("bugs");
                page.ClearWebsRequested += () => asked.Add("clear");

                Press(Find<Button>(page, buttonName));

                Assert.Equal(new[] { expected }, asked);
            });
        }
    }
}
