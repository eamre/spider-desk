using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Pomodoro;
using SpiderDesk.Infrastructure.Sessions;

namespace SpiderDesk.Infrastructure.Dialogs.Pages
{
    /// <summary>
    /// Ayarlar penceresinin "Pomodoro" sekmesi: geri sayım halkası, Başlat/Duraklat/Devam et ve Bitir düğmeleri, süre seçimi.
    /// Ne gösterileceğini <see cref="PomodoroDisplay"/> hesaplar; bu sekme yalnızca çizer ve düğmeleri olay olarak bildirir.
    /// </summary>
    public partial class PomodoroPage : UserControl, IPomodoroView
    {
        // Halkanın merkezi ve yarıçapı: 200×200'lük alanın ortası, kalınlığın yarısı kadar içeride
        private static readonly Point RingCenter = new(100, 100);
        private const double RingRadius = 95;

        private readonly Dictionary<PomodoroPreset, RadioButton> _presetButtons = new();
        private PomodoroPrimaryAction _primaryAction = PomodoroPrimaryAction.Start;
        private bool _isShowingPreset;
        private double _shownProgress = -1;

        public event Action? StartRequested;
        public event Action? PauseRequested;
        public event Action? ResumeRequested;
        public event Action? FinishRequested;
        public event Action<PomodoroPreset>? PresetSelected;

        public PomodoroPage()
        {
            InitializeComponent();

            PresetDescription.Text =
                $"Her {PomodoroPlan.FocusSessionsBeforeLongBreak} odak turundan sonra uzun mola verilir. Yeni süre bir sonraki turdan itibaren geçerli olur.";
            AddPreset("Klasik 25/5", PomodoroPreset.Classic);
            AddPreset("Uzun 50/10", PomodoroPreset.Extended);
        }

        public void ShowPreset(PomodoroPreset preset)
        {
            _isShowingPreset = true;
            try
            {
                _presetButtons[preset].IsChecked = true;
            }
            finally
            {
                _isShowingPreset = false;
            }
        }

        public void ShowState(PomodoroDisplay display)
        {
            // Her karede çağrılır: aynı değer yeniden verilince WPF hiçbir şeyi baştan çizmez
            TimeText.Text = display.Time;
            StatusText.Text = display.Status;

            _primaryAction = display.PrimaryAction;
            PrimaryButton.Content = display.PrimaryAction switch
            {
                PomodoroPrimaryAction.Pause => "Duraklat",
                PomodoroPrimaryAction.Resume => "Devam et",
                _ => "Başlat"
            };
            FinishButton.Visibility = display.CanFinish ? Visibility.Visible : Visibility.Collapsed;

            Brush ringBrush = (Brush)FindResource(display.Look == PomodoroLook.Active ? "AccentBrush" : "MutedTextBrush");
            ProgressArc.Stroke = ringBrush;
            StatusText.Foreground = display.Look == PomodoroLook.Active ? ringBrush : (Brush)FindResource("MutedTextBrush");
            TimeText.Foreground = (Brush)FindResource(display.Look == PomodoroLook.Idle ? "MutedTextBrush" : "TextBrush");

            // Halka yalnızca gözle görülür kadar değişince yeniden çizilir (yaklaşık yarım derece)
            if (Math.Abs(display.Progress - _shownProgress) > 0.0015)
            {
                _shownProgress = display.Progress;
                ProgressArc.Data = RingArc(display.Progress);
            }
        }

        private void OnPrimaryClicked(object sender, RoutedEventArgs e)
        {
            switch (_primaryAction)
            {
                case PomodoroPrimaryAction.Pause: PauseRequested?.Invoke(); break;
                case PomodoroPrimaryAction.Resume: ResumeRequested?.Invoke(); break;
                default: StartRequested?.Invoke(); break;
            }
        }

        private void OnFinishClicked(object sender, RoutedEventArgs e) => FinishRequested?.Invoke();

        private void AddPreset(string label, PomodoroPreset preset)
        {
            var button = new RadioButton { Content = label, GroupName = "PomodoroPreset", Style = (Style)FindResource("Segment") };
            button.Checked += (_, _) =>
            {
                if (!_isShowingPreset) PresetSelected?.Invoke(preset);
            };
            _presetButtons[preset] = button;
            PresetChoices.Children.Add(button);
        }

        /// <summary>
        /// Saatin 12'sinden başlayıp saat yönünde ilerleyen yay. Dolu (1) iken tam çember, boş (0) iken hiçbir şey çizilmez.
        /// </summary>
        public static Geometry? RingArc(double progress)
        {
            if (progress <= 0) return null;
            if (progress >= 0.9999) return new EllipseGeometry(RingCenter, RingRadius, RingRadius);

            double angle = progress * 2 * Math.PI;
            var start = new Point(RingCenter.X, RingCenter.Y - RingRadius);
            var end = new Point(RingCenter.X + RingRadius * Math.Sin(angle), RingCenter.Y - RingRadius * Math.Cos(angle));

            var figure = new PathFigure { StartPoint = start, IsClosed = false };
            figure.Segments.Add(new ArcSegment(end, new Size(RingRadius, RingRadius), 0, progress > 0.5, SweepDirection.Clockwise, true));
            var geometry = new PathGeometry(new[] { figure });
            geometry.Freeze();
            return geometry;
        }
    }
}
