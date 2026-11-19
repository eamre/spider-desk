using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Pomodoro;
using SpiderDesk.Infrastructure.Flyout.Pages;

namespace SpiderDesk.Tests.Infrastructure.Flyout.Pages
{
    /// <summary>
    /// "Pomodoro" sekmesinin kendisini doğrular: verilen durumu doğru çizdiğini ve düğmelerin doğru isteği bildirdiğini.
    /// </summary>
    public class PomodoroPageTests
    {
        private static readonly PomodoroDisplay Ready = new("25:00", "Hazır", 0, PomodoroPrimaryAction.Start, false, PomodoroLook.Idle);
        private static readonly PomodoroDisplay Focusing = new("12:30", "Odak · 1/4", 0.5, PomodoroPrimaryAction.Pause, true, PomodoroLook.Active);
        private static readonly PomodoroDisplay Paused = new("12:30", "Duraklatıldı", 0.5, PomodoroPrimaryAction.Resume, true, PomodoroLook.Paused);

        private static T Find<T>(PomodoroPage page, string name) where T : class => (T)page.FindName(name);

        private static void Press(Button button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        [Fact]
        public void ShowsTheTime_TheStatus_AndTheRightButtons()
        {
            StaThread.Run(() =>
            {
                var page = new PomodoroPage();

                page.ShowState(Focusing);

                Assert.Equal("12:30", Find<TextBlock>(page, "TimeText").Text);
                Assert.Equal("Odak · 1/4", Find<TextBlock>(page, "StatusText").Text);
                Assert.Equal("Duraklat", Find<Button>(page, "PrimaryButton").Content);
                Assert.Equal(Visibility.Visible, Find<Button>(page, "FinishButton").Visibility);
            });
        }

        [Fact]
        public void WhenReady_OnlyTheStartButtonIsShown_AndTheRingIsEmpty()
        {
            StaThread.Run(() =>
            {
                var page = new PomodoroPage();

                page.ShowState(Focusing);
                page.ShowState(Ready);

                Assert.Equal("Başlat", Find<Button>(page, "PrimaryButton").Content);
                Assert.Equal(Visibility.Collapsed, Find<Button>(page, "FinishButton").Visibility);
                Assert.Null(Find<Path>(page, "ProgressArc").Data);
            });
        }

        [Theory]
        [InlineData(PomodoroPrimaryAction.Start, "start")]
        [InlineData(PomodoroPrimaryAction.Pause, "pause")]
        [InlineData(PomodoroPrimaryAction.Resume, "resume")]
        public void TheBigButton_AsksForWhatItSays(PomodoroPrimaryAction action, string expected)
        {
            StaThread.Run(() =>
            {
                var page = new PomodoroPage();
                string? asked = null;
                page.StartRequested += () => asked = "start";
                page.PauseRequested += () => asked = "pause";
                page.ResumeRequested += () => asked = "resume";

                page.ShowState(Focusing with { PrimaryAction = action });
                Press(Find<Button>(page, "PrimaryButton"));

                Assert.Equal(expected, asked);
            });
        }

        [Fact]
        public void TheFinishButton_AsksToFinish()
        {
            StaThread.Run(() =>
            {
                var page = new PomodoroPage();
                bool finished = false;
                page.FinishRequested += () => finished = true;

                page.ShowState(Paused);
                Press(Find<Button>(page, "FinishButton"));

                Assert.True(finished);
            });
        }

        [Fact]
        public void ShowingThePreset_DoesNotReportIt_ButChoosingOneDoes()
        {
            StaThread.Run(() =>
            {
                var page = new PomodoroPage();
                PomodoroPreset? chosen = null;
                page.PresetSelected += preset => chosen = preset;

                page.ShowPreset(PomodoroPreset.Classic);
                Assert.Null(chosen);

                Find<Panel>(page, "PresetChoices").Children.OfType<RadioButton>().Single(b => (string)b.Content == "Uzun 50/10").IsChecked = true;
                Assert.Equal(PomodoroPreset.Extended, chosen);
            });
        }

        [Fact]
        public void TheRing_IsAFullCircleWhenFull_AndAnArcInBetween()
        {
            StaThread.Run(() =>
            {
                Assert.Null(PomodoroPage.RingArc(0));
                Assert.IsType<EllipseGeometry>(PomodoroPage.RingArc(1));

                // Yarım dolu yay: saatin 12'sinden başlar, 6'sında biter
                Rect half = PomodoroPage.RingArc(0.5)!.Bounds;
                Assert.Equal(100, half.Left, precision: 3);
                Assert.Equal(5, half.Top, precision: 3);
                Assert.Equal(195, half.Bottom, precision: 3);
            });
        }
    }
}
