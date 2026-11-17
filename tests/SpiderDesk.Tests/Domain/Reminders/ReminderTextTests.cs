using SpiderDesk.Domain.Reminders;

namespace SpiderDesk.Tests.Domain.Reminders
{
    public class ReminderTextTests
    {
        // 17 Kasım 2026, saat 10:00
        private static readonly DateTime Now = new(2026, 11, 17, 10, 0, 0);

        private static Reminder Due(DateTime dueAt, string text = "Annemi ara") => Reminder.Create(text, dueAt);

        [Theory]
        [InlineData(0, "⏰ Hatırlatıcılar")]
        [InlineData(1, "⏰ Hatırlatıcılar (1)")]
        [InlineData(3, "⏰ Hatırlatıcılar (3)")]
        public void MenuTitle_ShowsHowManyAreWaiting(int count, string expected)
        {
            Assert.Equal(expected, ReminderText.MenuTitleFor(count));
        }

        [Theory]
        [InlineData(5, "5 dk")]
        [InlineData(15, "15 dk")]
        [InlineData(30, "30 dk")]
        [InlineData(60, "1 saat")]
        [InlineData(90, "1,5 saat")]
        public void DelayLabel_IsShortAndTurkish(int minutes, string expected)
        {
            Assert.Equal(expected, ReminderText.DelayLabel(TimeSpan.FromMinutes(minutes)));
        }

        [Fact]
        public void DelayLabel_IsTurkish_EvenOnAnEnglishComputer()
        {
            var original = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
                Assert.Equal("1,5 saat", ReminderText.DelayLabel(TimeSpan.FromMinutes(90)));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void When_Today_SaysBugün()
        {
            Assert.Equal("Bugün 14:30", ReminderText.When(new DateTime(2026, 11, 17, 14, 30, 45), Now));
        }

        [Fact]
        public void When_Tomorrow_SaysYarın()
        {
            Assert.Equal("Yarın 09:05", ReminderText.When(new DateTime(2026, 11, 18, 9, 5, 0), Now));
        }

        [Fact]
        public void When_Yesterday_SaysDün()
        {
            Assert.Equal("Dün 22:00", ReminderText.When(new DateTime(2026, 11, 16, 22, 0, 0), Now));
        }

        [Fact]
        public void When_FurtherAway_ShowsTheDateInTurkish()
        {
            Assert.Equal("20 Kasım 09:00", ReminderText.When(new DateTime(2026, 11, 20, 9, 0, 0), Now));
        }

        [Fact]
        public void When_AfterMidnight_TomorrowIsCountedByDate_NotBy24Hours()
        {
            var lateEvening = new DateTime(2026, 11, 17, 23, 50, 0);

            Assert.Equal("Yarın 00:10", ReminderText.When(new DateTime(2026, 11, 18, 0, 10, 0), lateEvening));
        }

        [Fact]
        public void ListLabel_ShowsTheTimeAndTheText()
        {
            var reminder = Due(new DateTime(2026, 11, 17, 14, 30, 0));

            Assert.Equal("Bugün 14:30 · Annemi ara", ReminderText.ListLabel(reminder, Now));
        }

        [Fact]
        public void Notification_ForOneReminderOnTime_ShowsJustTheText()
        {
            var reminder = Due(Now);

            Assert.Equal(new ReminderNotification("⏰ Hatırlatma", "Annemi ara"), ReminderText.Notification(new[] { reminder }, Now));
        }

        [Fact]
        public void Notification_ForAMissedReminder_SaysItWasMissed_AndWhen()
        {
            var reminder = Due(new DateTime(2026, 11, 17, 8, 30, 0));

            var notification = ReminderText.Notification(new[] { reminder }, Now);

            Assert.Equal("⏰ Kaçırılan hatırlatma", notification.Title);
            Assert.Equal("Annemi ara (Bugün 08:30)", notification.Message);
        }

        [Fact]
        public void Notification_ForSeveralReminders_ListsThemAllInOne()
        {
            var due = new[]
            {
                Due(new DateTime(2026, 11, 16, 21, 0, 0), "Çöpü çıkar"),
                Due(Now, "Su iç")
            };

            var notification = ReminderText.Notification(due, Now);

            Assert.Equal("⏰ 2 hatırlatma", notification.Title);
            Assert.Equal($"• Çöpü çıkar (Dün 21:00){Environment.NewLine}• Su iç", notification.Message);
        }

        [Fact]
        public void Notification_WithNothingDue_IsAMistake()
        {
            Assert.Throws<ArgumentException>(() => ReminderText.Notification(Array.Empty<Reminder>(), Now));
        }

        [Fact]
        public void NoteText_OnTime_IsJustTheText()
        {
            Assert.Equal("Annemi ara", ReminderText.NoteText(Due(Now.AddSeconds(-30)), Now));
        }

        [Fact]
        public void NoteText_ForAMissedReminder_StartsWithKaçırdın()
        {
            Assert.Equal("Kaçırdın: Annemi ara", ReminderText.NoteText(Due(Now.AddHours(-2)), Now));
        }
    }
}
