using SpiderDesk.Domain.Reminders;

namespace SpiderDesk.Tests.Domain.Reminders
{
    public class ReminderTimeTests
    {
        // 1 Ocak 2026, saat 16:00:20
        private static readonly DateTime Now = new(2026, 1, 1, 16, 0, 20);

        [Fact]
        public void After_AddsTheDelayToNow()
        {
            Assert.Equal(new DateTime(2026, 1, 1, 16, 15, 20), ReminderTime.After(Now, TimeSpan.FromMinutes(15)));
        }

        [Fact]
        public void After_CanCrossMidnight()
        {
            var lateEvening = new DateTime(2026, 1, 1, 23, 50, 0);

            Assert.Equal(new DateTime(2026, 1, 2, 0, 20, 0), ReminderTime.After(lateEvening, TimeSpan.FromMinutes(30)));
        }

        [Fact]
        public void QuickDelays_Are5_15_30_And60Minutes()
        {
            Assert.Equal(new[] { 5.0, 15, 30, 60 }, ReminderTime.QuickDelays.Select(d => d.TotalMinutes));
        }

        [Fact]
        public void At_ALaterTimeToday_IsToday()
        {
            Assert.Equal(new DateTime(2026, 1, 1, 17, 30, 0), ReminderTime.At(Now, new TimeOnly(17, 30)));
        }

        [Fact]
        public void At_ATimeThatAlreadyPassedToday_IsTomorrow()
        {
            Assert.Equal(new DateTime(2026, 1, 2, 9, 0, 0), ReminderTime.At(Now, new TimeOnly(9, 0)));
        }

        [Fact]
        public void At_TheCurrentMinute_IsTomorrow()
        {
            // Saat 16:00:20'de "16:00" yazılırsa o an geçmiştir; hatırlatma hemen çalmak yerine yarına kurulur
            Assert.Equal(new DateTime(2026, 1, 2, 16, 0, 0), ReminderTime.At(Now, new TimeOnly(16, 0)));
        }

        [Fact]
        public void At_OneMinuteFromNow_IsToday()
        {
            Assert.Equal(new DateTime(2026, 1, 1, 16, 1, 0), ReminderTime.At(Now, new TimeOnly(16, 1)));
        }

        [Fact]
        public void At_OnTheLastDayOfTheYear_RollsIntoNextYear()
        {
            var newYearsEve = new DateTime(2026, 12, 31, 22, 0, 0);

            Assert.Equal(new DateTime(2027, 1, 1, 8, 0, 0), ReminderTime.At(newYearsEve, new TimeOnly(8, 0)));
        }

        [Fact]
        public void Resolve_WithOnlyAQuickDelay_UsesTheDelay()
        {
            Assert.Equal(Now.AddMinutes(15), ReminderTime.Resolve(Now, TimeSpan.FromMinutes(15), clockText: ""));
        }

        [Fact]
        public void Resolve_ATypedTime_WinsOverTheQuickDelay()
        {
            Assert.Equal(new DateTime(2026, 1, 1, 17, 30, 0), ReminderTime.Resolve(Now, TimeSpan.FromMinutes(15), "17:30"));
        }

        [Fact]
        public void Resolve_AnInvalidTypedTime_GivesNothing_EvenIfAQuickDelayIsSelected()
        {
            Assert.Null(ReminderTime.Resolve(Now, TimeSpan.FromMinutes(15), "25:00"));
        }

        [Fact]
        public void Resolve_WithNothingChosen_GivesNothing()
        {
            Assert.Null(ReminderTime.Resolve(Now, quickDelay: null, clockText: "   "));
        }

        [Theory]
        [InlineData("14:30", 14, 30)]
        [InlineData("14.30", 14, 30)]
        [InlineData("1430", 14, 30)]
        [InlineData("9:05", 9, 5)]
        [InlineData("09:05", 9, 5)]
        [InlineData("905", 9, 5)]
        [InlineData("9", 9, 0)]
        [InlineData("14", 14, 0)]
        [InlineData("0:00", 0, 0)]
        [InlineData("23:59", 23, 59)]
        [InlineData("  8:15  ", 8, 15)]
        public void TryParseClock_AcceptsCommonWaysOfWritingATime(string input, int hour, int minute)
        {
            Assert.True(ReminderTime.TryParseClock(input, out TimeOnly clock));
            Assert.Equal(new TimeOnly(hour, minute), clock);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("abc")]
        [InlineData("24:00")]
        [InlineData("12:60")]
        [InlineData("25")]
        [InlineData("12:5")]     // dakika iki haneli olmalı; "12:5" 12:05 mi 12:50 mi belli değil
        [InlineData("12:345")]
        [InlineData("12345")]
        [InlineData("-1:00")]
        [InlineData("12:-1")]
        [InlineData("1260")]
        public void TryParseClock_RejectsInvalidTimes(string? input)
        {
            Assert.False(ReminderTime.TryParseClock(input, out _));
        }
    }
}
