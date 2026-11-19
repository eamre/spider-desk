using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SpiderDesk.Domain.Reminders;
using SpiderDesk.Infrastructure.Sessions;

namespace SpiderDesk.Infrastructure.Dialogs.Pages
{
    /// <summary>
    /// Ayarlar penceresinin "Hatırlatıcılar" sekmesi: bekleyen hatırlatmaların listesi ve yeni hatırlatma kartı.
    /// Kart yalnızca girdiyi toplar; zamanı hesaplama ve metin kuralları Domain/Reminders içindedir.
    /// Kaydetmek ve zamanı gelince hatırlatmak <see cref="ReminderSessionController"/>'ın işidir.
    /// </summary>
    public partial class RemindersPage : UserControl, IReminderView
    {
        private static readonly TimeSpan DefaultQuickDelay = TimeSpan.FromMinutes(15);

        // "15 dk sonra" seçiliyken önizlemedeki saat, kart açık durdukça ilerlesin diye ara ara yenilenir
        private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromSeconds(10) };
        private readonly List<RadioButton> _quickDelayButtons = new();
        private TimeSpan? _selectedQuickDelay = DefaultQuickDelay;

        public event Action<Reminder>? ReminderCreated;
        public event Action<Guid>? DeleteRequested;

        public RemindersPage()
        {
            InitializeComponent();
            NoteInput.MaxLength = Reminder.MaxTextLength;
            AddQuickDelayButtons();
            _previewTimer.Tick += (_, _) => UpdateAddCard();
        }

        public void ShowReminders(IReadOnlyList<ReminderListItem> reminders)
        {
            ReminderList.Children.Clear();
            foreach (ReminderListItem reminder in reminders)
            {
                ReminderList.Children.Add(BuildRow(reminder));
            }
            EmptyState.Visibility = reminders.Count == 0 && AddCard.Visibility != Visibility.Visible
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ---------- Yeni hatırlatma kartı ----------

        private void OnAddClicked(object sender, RoutedEventArgs e) => OpenAddCard();

        private void OnCancelClicked(object sender, RoutedEventArgs e) => CloseAddCard();

        private void OpenAddCard()
        {
            NoteInput.Text = string.Empty;
            ClockInput.Text = string.Empty;
            _quickDelayButtons.Find(b => (TimeSpan)b.Tag == DefaultQuickDelay)!.IsChecked = true;
            _selectedQuickDelay = DefaultQuickDelay;

            AddCard.Visibility = Visibility.Visible;
            AddButton.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;
            UpdateAddCard();
            _previewTimer.Start();
            NoteInput.Focus();
        }

        private void CloseAddCard()
        {
            _previewTimer.Stop();
            AddCard.Visibility = Visibility.Collapsed;
            AddButton.Visibility = Visibility.Visible;
            EmptyState.Visibility = ReminderList.Children.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddQuickDelayButtons()
        {
            foreach (TimeSpan delay in ReminderTime.QuickDelays)
            {
                var button = new RadioButton
                {
                    Content = ReminderText.DelayLabel(delay),
                    GroupName = "QuickDelay",
                    Tag = delay,
                    Style = (Style)FindResource("Segment"),
                    IsChecked = delay == DefaultQuickDelay
                };
                button.Checked += (_, _) =>
                {
                    _selectedQuickDelay = delay;
                    // Hızlı süre seçilince yazılmış saat silinir; ikisi aynı anda geçerli olamaz
                    ClockInput.Text = string.Empty;
                    UpdateAddCard();
                };
                _quickDelayButtons.Add(button);
                QuickDelayChoices.Children.Add(button);
            }
        }

        private void OnInputChanged(object sender, TextChangedEventArgs e) => UpdateAddCard();

        private void OnClockChanged(object sender, TextChangedEventArgs e)
        {
            // Yalnızca rakam yazılır, ayraç kendiliğinden gelir: "1959" → "19:59"
            string formatted = ClockInputFormat.Format(ClockInput.Text);
            if (formatted != ClockInput.Text)
            {
                ClockInput.Text = formatted;   // bu atama bu metodu yeniden çağırır; ikinci çağrıda metin değişmez
                ClockInput.CaretIndex = formatted.Length;
                return;
            }

            ClockPlaceholder.Visibility = ClockInput.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(ClockInput.Text))
            {
                // Saat yazılınca hızlı süre seçimi kalkar; geçerli olan yazılan saattir
                _selectedQuickDelay = null;
                foreach (RadioButton button in _quickDelayButtons) button.IsChecked = false;
            }
            UpdateAddCard();
        }

        /// <summary>Karakter sayacını, zaman önizlemesini ve "Kaydet" düğmesinin basılabilir olup olmadığını günceller.</summary>
        private void UpdateAddCard()
        {
            DateTime now = DateTime.Now;
            DateTime? dueAt = ReminderTime.Resolve(now, _selectedQuickDelay, ClockInput.Text);

            CharacterCount.Text = $"{NoteInput.Text.Length}/{Reminder.MaxTextLength}";

            if (dueAt is { } due)
            {
                DuePreview.Text = ReminderText.When(due, now);
                DuePreview.Foreground = (Brush)FindResource("AccentBrush");
            }
            else
            {
                DuePreview.Text = string.IsNullOrWhiteSpace(ClockInput.Text) ? "Bir süre seç ya da saat yaz" : "Saati 14:30 gibi yaz";
                DuePreview.Foreground = (Brush)FindResource("WarningIconBrush");
            }

            SaveButton.IsEnabled = dueAt != null && Reminder.IsValidText(NoteInput.Text);
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            // "15 dk sonra", kaydete basılan andan itibaren sayılır
            DateTime? dueAt = ReminderTime.Resolve(DateTime.Now, _selectedQuickDelay, ClockInput.Text);
            if (dueAt is not { } due || !Reminder.IsValidText(NoteInput.Text)) return;

            CloseAddCard();
            ReminderCreated?.Invoke(Reminder.Create(NoteInput.Text, due));
        }

        // ---------- Liste ----------

        /// <summary>Bir hatırlatma satırı: solda mavi hap içinde zamanı, ortada metni, sağda silme düğmesi.</summary>
        private Border BuildRow(ReminderListItem reminder)
        {
            var when = new Border
            {
                Background = (Brush)FindResource("AccentTintBrush"),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = reminder.When, FontSize = 12, Foreground = (Brush)FindResource("AccentBrush") }
            };

            var text = new TextBlock
            {
                Text = reminder.Text,
                Foreground = (Brush)FindResource("TextBrush"),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 8, 0)
            };

            var delete = new Button { Style = (Style)FindResource("DeleteIconButton"), Tag = reminder.Id };
            delete.Click += (_, _) => DeleteRequested?.Invoke(reminder.Id);

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(text, 1);
            Grid.SetColumn(delete, 2);
            row.Children.Add(when);
            row.Children.Add(text);
            row.Children.Add(delete);

            var card = new Border { Style = (Style)FindResource("Card"), Padding = new Thickness(12, 10, 8, 10), Child = row };
            return card;
        }
    }
}
