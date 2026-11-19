using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SpiderDesk.Domain.Reminders;
using SpiderDesk.Infrastructure.Dialogs.Pages;
using SpiderDesk.Infrastructure.Sessions;

namespace SpiderDesk.Tests.Infrastructure.Dialogs.Pages
{
    /// <summary>
    /// "Hatırlatıcılar" sekmesinin kendisini doğrular: liste ve boş durum, ekleme kartının kuralları
    /// (metin ve geçerli bir zaman olmadan kaydedilemez) ve silme düğmesi.
    /// </summary>
    public class RemindersPageTests
    {
        private static T Find<T>(RemindersPage page, string name) where T : class => (T)page.FindName(name);

        private static void Press(Button button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        private static RadioButton QuickDelay(RemindersPage page, string label) =>
            Find<Panel>(page, "QuickDelayChoices").Children.OfType<RadioButton>().Single(b => (string)b.Content == label);

        private static RemindersPage PageWithOpenCard()
        {
            var page = new RemindersPage();
            page.ShowReminders(Array.Empty<ReminderListItem>());
            Press(Find<Button>(page, "AddButton"));
            return page;
        }

        [Fact]
        public void WithNoReminders_ShowsTheEmptyState()
        {
            StaThread.Run(() =>
            {
                var page = new RemindersPage();

                page.ShowReminders(Array.Empty<ReminderListItem>());

                Assert.Equal(Visibility.Visible, Find<FrameworkElement>(page, "EmptyState").Visibility);
                Assert.Empty(Find<Panel>(page, "ReminderList").Children);
            });
        }

        [Fact]
        public void WithReminders_ShowsOneRowEach_AndHidesTheEmptyState()
        {
            StaThread.Run(() =>
            {
                var page = new RemindersPage();

                page.ShowReminders(new[]
                {
                    new ReminderListItem(Guid.NewGuid(), "Bugün 14:30", "Toplantıya hazırlan"),
                    new ReminderListItem(Guid.NewGuid(), "Yarın 09:00", "Faturayı öde")
                });

                Assert.Equal(2, Find<Panel>(page, "ReminderList").Children.Count);
                Assert.Equal(Visibility.Collapsed, Find<FrameworkElement>(page, "EmptyState").Visibility);
            });
        }

        [Fact]
        public void TheDeleteButton_AsksToDeleteThatReminder()
        {
            StaThread.Run(() =>
            {
                var page = new RemindersPage();
                var id = Guid.NewGuid();
                Guid? deleted = null;
                page.DeleteRequested += value => deleted = value;
                page.ShowReminders(new[] { new ReminderListItem(id, "Bugün 14:30", "Su iç") });

                var row = (Border)Find<Panel>(page, "ReminderList").Children[0];
                Button delete = ((Grid)row.Child).Children.OfType<Button>().Single();
                Press(delete);

                Assert.Equal(id, deleted);
            });
        }

        [Fact]
        public void OpeningTheCard_HidesTheAddButton_AndStartsWithFifteenMinutes()
        {
            StaThread.Run(() =>
            {
                RemindersPage page = PageWithOpenCard();

                Assert.Equal(Visibility.Visible, Find<FrameworkElement>(page, "AddCard").Visibility);
                Assert.Equal(Visibility.Collapsed, Find<FrameworkElement>(page, "AddButton").Visibility);
                Assert.Equal(Visibility.Collapsed, Find<FrameworkElement>(page, "EmptyState").Visibility);
                Assert.True(QuickDelay(page, "15 dk").IsChecked);
            });
        }

        [Fact]
        public void WithoutText_TheReminderCannotBeSaved()
        {
            StaThread.Run(() =>
            {
                RemindersPage page = PageWithOpenCard();

                Assert.False(Find<Button>(page, "SaveButton").IsEnabled);

                Find<TextBox>(page, "NoteInput").Text = "Su iç";
                Assert.True(Find<Button>(page, "SaveButton").IsEnabled);
            });
        }

        [Fact]
        public void AWrongClock_CannotBeSaved_AndSaysHowToWriteIt()
        {
            StaThread.Run(() =>
            {
                RemindersPage page = PageWithOpenCard();
                Find<TextBox>(page, "NoteInput").Text = "Su iç";

                Find<TextBox>(page, "ClockInput").Text = "25:99";

                Assert.False(Find<Button>(page, "SaveButton").IsEnabled);
                Assert.Equal("Saati 14:30 gibi yaz", Find<TextBlock>(page, "DuePreview").Text);
                // Saat yazılınca hızlı süre seçimi kalkar
                Assert.False(QuickDelay(page, "15 dk").IsChecked);
            });
        }

        [Fact]
        public void TypingOnlyDigits_PutsTheSeparatorInByItself_AndHidesTheHint()
        {
            StaThread.Run(() =>
            {
                RemindersPage page = PageWithOpenCard();
                var clock = Find<TextBox>(page, "ClockInput");
                Assert.Equal(Visibility.Visible, Find<FrameworkElement>(page, "ClockPlaceholder").Visibility);

                clock.Text = "195";
                Assert.Equal("19:5", clock.Text);

                clock.Text = "1959";
                Assert.Equal("19:59", clock.Text);
                Assert.Equal(clock.Text.Length, clock.CaretIndex);
                Assert.Equal(Visibility.Collapsed, Find<FrameworkElement>(page, "ClockPlaceholder").Visibility);
                Assert.EndsWith("19:59", Find<TextBlock>(page, "DuePreview").Text);
            });
        }

        [Fact]
        public void ClearingTheClock_BringsTheHintBack()
        {
            StaThread.Run(() =>
            {
                RemindersPage page = PageWithOpenCard();
                var clock = Find<TextBox>(page, "ClockInput");

                clock.Text = "14";
                clock.Text = string.Empty;

                Assert.Equal(Visibility.Visible, Find<FrameworkElement>(page, "ClockPlaceholder").Visibility);
            });
        }

        [Fact]
        public void Saving_CreatesTheReminder_AndClosesTheCard()
        {
            StaThread.Run(() =>
            {
                RemindersPage page = PageWithOpenCard();
                Reminder? created = null;
                page.ReminderCreated += reminder => created = reminder;
                Find<TextBox>(page, "NoteInput").Text = "Su iç";
                QuickDelay(page, "30 dk").IsChecked = true;

                DateTime before = DateTime.Now;
                Press(Find<Button>(page, "SaveButton"));

                Assert.NotNull(created);
                Assert.Equal("Su iç", created!.Text);
                Assert.InRange(created.DueAt, before.AddMinutes(30), DateTime.Now.AddMinutes(30));
                Assert.Equal(Visibility.Collapsed, Find<FrameworkElement>(page, "AddCard").Visibility);
                Assert.Equal(Visibility.Visible, Find<FrameworkElement>(page, "AddButton").Visibility);
            });
        }

        [Fact]
        public void Cancelling_ClosesTheCard_WithoutCreatingAnything()
        {
            StaThread.Run(() =>
            {
                RemindersPage page = PageWithOpenCard();
                bool created = false;
                page.ReminderCreated += _ => created = true;
                Find<TextBox>(page, "NoteInput").Text = "Su iç";

                Press(Find<Button>(page, "CancelButton"));

                Assert.False(created);
                Assert.Equal(Visibility.Collapsed, Find<FrameworkElement>(page, "AddCard").Visibility);
                Assert.Equal(Visibility.Visible, Find<FrameworkElement>(page, "EmptyState").Visibility);
            });
        }

        [Fact]
        public void ReopeningTheCard_StartsEmpty()
        {
            StaThread.Run(() =>
            {
                RemindersPage page = PageWithOpenCard();
                Find<TextBox>(page, "NoteInput").Text = "Yarım kalan";
                Find<TextBox>(page, "ClockInput").Text = "14:30";
                Press(Find<Button>(page, "CancelButton"));

                Press(Find<Button>(page, "AddButton"));

                Assert.Equal(string.Empty, Find<TextBox>(page, "NoteInput").Text);
                Assert.Equal(string.Empty, Find<TextBox>(page, "ClockInput").Text);
                Assert.True(QuickDelay(page, "15 dk").IsChecked);
            });
        }
    }
}
