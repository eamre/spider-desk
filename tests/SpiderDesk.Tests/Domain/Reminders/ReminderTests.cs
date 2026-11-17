using SpiderDesk.Domain.Reminders;

namespace SpiderDesk.Tests.Domain.Reminders
{
    public class ReminderTests
    {
        private static readonly DateTime Due = new(2026, 1, 1, 14, 30, 0);

        [Fact]
        public void Create_TrimsTheText_AndKeepsTheTime()
        {
            var reminder = Reminder.Create("  Annemi ara  ", Due);

            Assert.Equal("Annemi ara", reminder.Text);
            Assert.Equal(Due, reminder.DueAt);
        }

        [Fact]
        public void Create_TurnsLineBreaksIntoSpaces()
        {
            var reminder = Reminder.Create("Sunumu\r\nunutma", Due);

            Assert.Equal("Sunumu unutma", reminder.Text);
        }

        [Fact]
        public void Create_GivesEveryReminderItsOwnId()
        {
            var first = Reminder.Create("Su iç", Due);
            var second = Reminder.Create("Su iç", Due);

            Assert.NotEqual(first.Id, second.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void EmptyText_IsNotValid(string? text)
        {
            Assert.False(Reminder.IsValidText(text));
            Assert.Throws<ArgumentException>(() => Reminder.Create(text!, Due));
        }

        [Fact]
        public void TextUpToTheLimit_IsValid_ButLongerTextIsNot()
        {
            string longest = new('a', Reminder.MaxTextLength);

            Assert.True(Reminder.IsValidText(longest));
            Assert.False(Reminder.IsValidText(longest + "a"));
            Assert.Throws<ArgumentException>(() => Reminder.Create(longest + "a", Due));
        }

        [Fact]
        public void SpacesAroundTheText_DoNotCountTowardsTheLimit()
        {
            string longest = new('a', Reminder.MaxTextLength);

            Assert.True(Reminder.IsValidText("  " + longest + "  "));
        }

        [Theory]
        [InlineData(0, false)]    // tam zamanında
        [InlineData(40, false)]   // örümcek tavana tırmanırken biraz gecikti
        [InlineData(60, false)]   // sınırın tam üstü hâlâ kaçırılmış sayılmaz
        [InlineData(61, true)]
        [InlineData(3600, true)]  // bilgisayar o saatte kapalıydı
        public void IsMissed_OnlyWhenShownMoreThanAMinuteLate(int secondsLate, bool expected)
        {
            var reminder = Reminder.Create("Annemi ara", Due);

            Assert.Equal(expected, reminder.IsMissedAt(Due.AddSeconds(secondsLate)));
        }
    }
}
