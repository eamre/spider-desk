using SpiderDesk.Domain.Reminders;

namespace SpiderDesk.Tests.Domain.Reminders
{
    public class ReminderScheduleTests
    {
        // Testlerde gerçek saat yerine sabit bir an kullanıyoruz; "zaman geçti" demek için dakika ekliyoruz
        private static readonly DateTime T0 = new(2026, 1, 1, 9, 0, 0);

        private static DateTime At(double minutes) => T0.AddMinutes(minutes);

        private static Reminder ReminderAt(double minutes, string text = "Not") => Reminder.Create(text, At(minutes));

        private static string[] Texts(IEnumerable<Reminder> reminders) => reminders.Select(r => r.Text).ToArray();

        [Fact]
        public void NewSchedule_IsEmpty()
        {
            var schedule = new ReminderSchedule();

            Assert.Empty(schedule.Upcoming);
            Assert.Empty(schedule.TakeDue(At(1000)));
        }

        [Fact]
        public void Upcoming_IsSortedByTime_NotByTheOrderTheyWereAdded()
        {
            var schedule = new ReminderSchedule();
            schedule.Add(ReminderAt(30, "üçüncü"));
            schedule.Add(ReminderAt(10, "birinci"));
            schedule.Add(ReminderAt(20, "ikinci"));

            Assert.Equal(new[] { "birinci", "ikinci", "üçüncü" }, Texts(schedule.Upcoming));
        }

        [Fact]
        public void RemindersAtTheSameTime_KeepTheOrderTheyWereAdded()
        {
            var schedule = new ReminderSchedule();
            schedule.Add(ReminderAt(10, "önce eklenen"));
            schedule.Add(ReminderAt(10, "sonra eklenen"));

            Assert.Equal(new[] { "önce eklenen", "sonra eklenen" }, Texts(schedule.Upcoming));
        }

        [Fact]
        public void SavedReminders_AreLoadedInTimeOrder()
        {
            var schedule = new ReminderSchedule(new[] { ReminderAt(20, "ikinci"), ReminderAt(10, "birinci") });

            Assert.Equal(new[] { "birinci", "ikinci" }, Texts(schedule.Upcoming));
        }

        [Fact]
        public void TakeDue_BeforeTheTime_GivesNothing()
        {
            var schedule = new ReminderSchedule();
            schedule.Add(ReminderAt(10));

            Assert.Empty(schedule.TakeDue(At(9.99)));
            Assert.Single(schedule.Upcoming);
        }

        [Fact]
        public void TakeDue_AtTheExactTime_GivesTheReminder_AndRemovesIt()
        {
            var schedule = new ReminderSchedule();
            var reminder = ReminderAt(10);
            schedule.Add(reminder);

            Assert.Equal(new[] { reminder }, schedule.TakeDue(At(10)));
            Assert.Empty(schedule.Upcoming);
            Assert.Empty(schedule.TakeDue(At(11)));
        }

        [Fact]
        public void TakeDue_AfterALongSleep_GivesAllPassedReminders_OldestFirst_AndKeepsTheRest()
        {
            var schedule = new ReminderSchedule();
            schedule.Add(ReminderAt(120, "gelecek"));
            schedule.Add(ReminderAt(20, "ikinci"));
            schedule.Add(ReminderAt(10, "birinci"));

            var due = schedule.TakeDue(At(60));

            Assert.Equal(new[] { "birinci", "ikinci" }, Texts(due));
            Assert.Equal(new[] { "gelecek" }, Texts(schedule.Upcoming));
        }

        [Fact]
        public void Remove_DeletesOnlyThatReminder_EvenIfAnotherHasTheSameTextAndTime()
        {
            var schedule = new ReminderSchedule();
            var first = ReminderAt(10, "Su iç");
            var second = ReminderAt(10, "Su iç");
            schedule.Add(first);
            schedule.Add(second);

            Assert.True(schedule.Remove(first.Id));

            Assert.Equal(new[] { second }, schedule.Upcoming);
        }

        [Fact]
        public void Remove_UnknownReminder_ReturnsFalse()
        {
            var schedule = new ReminderSchedule();
            schedule.Add(ReminderAt(10));

            Assert.False(schedule.Remove(Guid.NewGuid()));
            Assert.Single(schedule.Upcoming);
        }
    }
}
