using System.Windows;
using System.Windows.Media;
using SpiderDesk.Domain.Reminders;
using SpiderDesk.Rendering;

namespace SpiderDesk.Tests.Rendering
{
    /// <summary>
    /// Örümceğin tuttuğu notta yazının hiçbir zaman kesilmediğini doğrular: boşluksuz uzun kelimeler de
    /// sığmadıkları yerden alt satıra geçer, kısa kelimeler ise ortadan bölünmez.
    /// </summary>
    public class NoteTextLayoutTests
    {
        private const char BreakOpportunity = '​';

        private static FormattedText Layout(string text) => NoteTextLayout.Create(text, Brushes.Black, 1.0);

        [Theory]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData("Toplantıdanöncesunumdosyasınıgözdengeçirveali'yiaramayıunutma")]
        [InlineData("https://www.ornek-bir-adres.com/cok/uzun/bir/yol/sayfa.html")]
        [InlineData("Kaçırdın: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        public void ALongWordWithoutSpaces_IsSplitOverLines_InsteadOfBeingCut(string text)
        {
            StaThread.Run(() =>
            {
                FormattedText layout = Layout(text);

                // En uzun bölünemez parça bile satıra sığıyor: hiçbir şey taşmıyor ya da "..." ile kesilmiyor
                Assert.True(layout.MinWidth <= NoteTextLayout.MaxTextWidth, $"En dar hâli {layout.MinWidth}");
                Assert.True(layout.Width <= NoteTextLayout.MaxTextWidth);
                Assert.Equal(TextTrimming.None, layout.Trimming);
                // Bütün harfler duruyor
                Assert.Equal(text, layout.Text.Replace(BreakOpportunity.ToString(), ""));
            });
        }

        [Fact]
        public void TheLongestReminder_FitsWithoutLosingALetter()
        {
            StaThread.Run(() =>
            {
                string longest = ReminderText.MissedNotePrefix + new string('m', Reminder.MaxTextLength);

                FormattedText layout = Layout(longest);

                Assert.True(layout.MinWidth <= NoteTextLayout.MaxTextWidth);
                Assert.True(layout.Height > 3 * NoteTextLayout.FontSize, "Uzun yazı birkaç satıra yayılmalı");
            });
        }

        [Fact]
        public void ANormalSentence_IsLeftAsItIs_SoWordsAreNotSplitInTheMiddle()
        {
            StaThread.Run(() =>
            {
                const string sentence = "Toplantıdan önce sunum dosyasını gözden geçir ve Ali'yi ara";

                FormattedText layout = Layout(sentence);

                Assert.Equal(sentence, layout.Text);
            });
        }

        [Fact]
        public void InAMixedSentence_OnlyTheTooLongWordIsMadeBreakable()
        {
            StaThread.Run(() =>
            {
                string longWord = new string('a', 40);

                FormattedText layout = Layout($"Su iç {longWord}");

                Assert.StartsWith("Su iç ", layout.Text);
                Assert.Contains(BreakOpportunity, layout.Text);
            });
        }
    }
}
