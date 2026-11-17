using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SpiderDesk.Domain.Reminders;

namespace SpiderDesk.Infrastructure.Dialogs
{
    /// <summary>
    /// Yeni hatırlatma eklenen küçük pencere: metin, hızlı süre seçenekleri ya da bir saat.
    /// Pencere yalnızca girdiyi toplar; zamanı hesaplama ve metin kuralları Domain/Reminders içindedir.
    /// </summary>
    public partial class ReminderWindow : Window
    {
        private static readonly TimeSpan DefaultQuickDelay = TimeSpan.FromMinutes(15);
        private static readonly Brush PreviewBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x4E, 0x6B, 0x2F)));
        private static readonly Brush WarningBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xB0, 0x3A, 0x2E)));

        // "15 dk sonra" seçiliyken önizlemedeki saat, pencere açık durdukça ilerlesin diye ara ara yenilenir
        private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromSeconds(10) };
        private TimeSpan? _selectedQuickDelay = DefaultQuickDelay;

        /// <summary>Kullanıcı "Kaydet"e basınca oluşturulan hatırlatma.</summary>
        public event Action<Reminder>? ReminderCreated;

        public ReminderWindow()
        {
            InitializeComponent();
            NoteInput.MaxLength = Reminder.MaxTextLength;
            AddQuickDelayButtons();

            _previewTimer.Tick += (_, _) => UpdateState();
            Loaded += (_, _) =>
            {
                NoteInput.Focus();
                _previewTimer.Start();
            };
            Closed += (_, _) => _previewTimer.Stop();

            UpdateState();
        }

        private void AddQuickDelayButtons()
        {
            foreach (TimeSpan delay in ReminderTime.QuickDelays)
            {
                var button = new RadioButton
                {
                    Content = ReminderText.DelayLabel(delay),
                    GroupName = "QuickDelay",
                    Style = (Style)FindResource("QuickDelayButton"),
                    IsChecked = delay == DefaultQuickDelay
                };
                button.Checked += (_, _) =>
                {
                    _selectedQuickDelay = delay;
                    // Hızlı süre seçilince yazılmış saat silinir; ikisi aynı anda geçerli olamaz
                    ClockInput.Text = string.Empty;
                    UpdateState();
                };
                QuickDelayPanel.Children.Add(button);
            }
        }

        private void OnInputChanged(object sender, TextChangedEventArgs e) => UpdateState();

        private void OnClockChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ClockInput.Text))
            {
                // Saat yazılınca hızlı süre seçimi kalkar; geçerli olan yazılan saattir
                _selectedQuickDelay = null;
                foreach (var child in QuickDelayPanel.Children)
                {
                    if (child is RadioButton button) button.IsChecked = false;
                }
            }

            UpdateState();
        }

        /// <summary>Karakter sayacını, zaman önizlemesini ve "Kaydet" düğmesinin açık olup olmadığını günceller.</summary>
        private void UpdateState()
        {
            DateTime now = DateTime.Now;
            DateTime? dueAt = ReminderTime.Resolve(now, _selectedQuickDelay, ClockInput.Text);

            CharacterCount.Text = $"{NoteInput.Text.Length}/{Reminder.MaxTextLength}";

            if (dueAt is { } due)
            {
                DuePreview.Text = $"⏰ {ReminderText.When(due, now)}";
                DuePreview.Foreground = PreviewBrush;
            }
            else
            {
                DuePreview.Text = string.IsNullOrWhiteSpace(ClockInput.Text)
                    ? "Bir süre seç ya da saat yaz"
                    : "Saati 14:30 gibi yaz";
                DuePreview.Foreground = WarningBrush;
            }

            SaveButton.IsEnabled = dueAt != null && Reminder.IsValidText(NoteInput.Text);
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            // "15 dk sonra", kaydete basılan andan itibaren sayılır
            DateTime? dueAt = ReminderTime.Resolve(DateTime.Now, _selectedQuickDelay, ClockInput.Text);
            if (dueAt is not { } due || !Reminder.IsValidText(NoteInput.Text)) return;

            ReminderCreated?.Invoke(Reminder.Create(NoteInput.Text, due));
            Close();
        }

        private static Brush Freeze(Brush brush)
        {
            brush.Freeze();
            return brush;
        }
    }
}
