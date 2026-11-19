using SpiderDesk.Domain.Reminders;

namespace SpiderDesk.Tests.Domain.Reminders
{
    /// <summary>
    /// Saat kutusuna yazılanın "19:59" biçimine girdiğini doğrular: rakamlar yazılırken ayraç kendiliğinden gelir,
    /// silerken de rakamla birlikte gider.
    /// </summary>
    public class ClockInputFormatTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("1", "1")]
        [InlineData("19", "19")]
        [InlineData("195", "19:5")]
        [InlineData("1959", "19:59")]
        public void TypingDigits_AddsTheSeparatorByItself(string typed, string expected)
        {
            Assert.Equal(expected, ClockInputFormat.Format(typed));
        }

        [Theory]
        [InlineData("19:5", "19:5")]
        [InlineData("19:59", "19:59")]
        [InlineData("19:595", "19:59")]
        [InlineData("19595", "19:59")]
        public void NoMoreThanFourDigits(string typed, string expected)
        {
            Assert.Equal(expected, ClockInputFormat.Format(typed));
        }

        [Fact]
        public void DeletingTheFirstMinuteDigit_AlsoRemovesTheSeparator()
        {
            // Kutuda "19:5" varken bir kez silmek "19:" bırakır; ayraç da gider
            Assert.Equal("19", ClockInputFormat.Format("19:"));
        }

        [Theory]
        [InlineData("9.", "9:")]
        [InlineData("9:", "9:")]
        [InlineData("9.3", "9:3")]
        [InlineData("9.30", "9:30")]
        public void ASingleDigitHour_CanBeFinishedWithTheSeparatorKey(string typed, string expected)
        {
            Assert.Equal(expected, ClockInputFormat.Format(typed));
        }

        [Theory]
        [InlineData("19.59", "19:59")]
        [InlineData("1a9b5c9", "19:59")]
        [InlineData(":30", "30")]
        [InlineData("abc", "")]
        [InlineData(null, "")]
        public void LettersAreDropped_AndADotWorksLikeAColon(string? typed, string expected)
        {
            Assert.Equal(expected, ClockInputFormat.Format(typed));
        }

        [Theory]
        [InlineData("1959")]
        [InlineData("9.30")]
        [InlineData("0700")]
        public void TheResult_IsReadByTheClockParser(string typed)
        {
            Assert.True(ReminderTime.TryParseClock(ClockInputFormat.Format(typed), out _));
        }

        [Theory]
        [InlineData("19:59")]
        [InlineData("9:")]
        [InlineData("19:5")]
        public void FormattingTwice_ChangesNothing(string formatted)
        {
            Assert.Equal(formatted, ClockInputFormat.Format(formatted));
        }
    }
}
