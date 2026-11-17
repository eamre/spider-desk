using SpiderDesk.Domain.Pomodoro;

namespace SpiderDesk.Tests.Domain.Pomodoro
{
    public class PomodoroTextTests
    {
        [Theory]
        [InlineData(PomodoroPhase.Focus, 25 * 60.0, "Odak 25:00")]
        [InlineData(PomodoroPhase.Focus, 18 * 60 + 32.0, "Odak 18:32")]
        [InlineData(PomodoroPhase.Focus, 0.2, "Odak 00:01")]
        [InlineData(PomodoroPhase.Focus, 0, "Odak 00:00")]
        [InlineData(PomodoroPhase.ShortBreak, 299.4, "Mola 05:00")]
        [InlineData(PomodoroPhase.LongBreak, 50 * 60.0, "Uzun Mola 50:00")]
        public void Countdown_ShowsPhaseAndMinutesSeconds(PomodoroPhase phase, double seconds, string expected)
        {
            Assert.Equal(expected, PomodoroText.Countdown(phase, TimeSpan.FromSeconds(seconds)));
        }

        [Fact]
        public void Countdown_NeverShowsNegativeTime()
        {
            Assert.Equal("Odak 00:00", PomodoroText.Countdown(PomodoroPhase.Focus, TimeSpan.FromSeconds(-5)));
        }

        [Fact]
        public void FocusAnnouncement_WritesFocus_AndMentionsItsLength()
        {
            var announcement = PomodoroText.AnnouncementFor(PomodoroPhase.Focus, TimeSpan.FromMinutes(25));

            Assert.Equal(PomodoroText.FocusWord, announcement.SilkWord);
            Assert.Contains("25 dakika", announcement.Message);
        }

        [Theory]
        [InlineData(PomodoroPhase.ShortBreak, 5)]
        [InlineData(PomodoroPhase.LongBreak, 15)]
        public void BreakAnnouncements_WriteBreak_AndMentionTheirLength(PomodoroPhase phase, int minutes)
        {
            var announcement = PomodoroText.AnnouncementFor(phase, TimeSpan.FromMinutes(minutes));

            Assert.Equal(PomodoroText.BreakWord, announcement.SilkWord);
            Assert.Contains($"{minutes} dakika", announcement.Message);
        }

        [Fact]
        public void LongBreakAnnouncement_SaysHowManySessionsWereDone()
        {
            var announcement = PomodoroText.AnnouncementFor(PomodoroPhase.LongBreak, TimeSpan.FromMinutes(15));

            Assert.Contains($"{PomodoroPlan.FocusSessionsBeforeLongBreak} tur", announcement.Message);
        }
    }
}
