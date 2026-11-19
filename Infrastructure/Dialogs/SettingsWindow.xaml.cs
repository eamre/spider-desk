using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SpiderDesk.Infrastructure.Dialogs
{
    /// <summary>Ayarlar penceresinin sekmeleri.</summary>
    public enum SettingsTab
    {
        Spider,
        Pomodoro,
        Reminders,
        Settings
    }

    /// <summary>
    /// Tepsi ikonuna tıklayınca tepsinin hemen üstünde açılan pencere (flyout): solda sekmeler, sağda seçili sekmenin içeriği.
    /// Uygulama boyunca tek bir tane vardır; kapatılınca ya da dışına tıklanınca yok olmaz, gizlenir.
    /// Örümcek bu sırada çalışmaya devam eder.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        // XAML'deki dış boşluk: pencerenin çevresindeki gölgeye ayrılan yer
        private const double ShadowMargin = 16;
        private static readonly Size FlyoutSize = new(520, 640);

        private DateTime? _lastAutoHiddenAt;

        public SettingsTab CurrentTab { get; private set; }

        /// <summary>"Ayarlar" sekmesi; <see cref="Sessions.SettingsSessionController"/> buna bağlanır.</summary>
        public Pages.SettingsPage SettingsPage => SettingsPageView;

        /// <summary>"Pomodoro" sekmesi; <see cref="Sessions.PomodoroSessionController"/> buna bağlanır.</summary>
        public Pages.PomodoroPage PomodoroPage => PomodoroPageView;

        /// <summary>"Hatırlatıcılar" sekmesi; <see cref="Sessions.ReminderSessionController"/> buna bağlanır.</summary>
        public Pages.RemindersPage RemindersPage => RemindersPageView;

        /// <summary>
        /// True ise pencere her açılışta tepsinin yanına yerleşir. Testler pencereyi ekranın dışında tutmak için kapatır.
        /// </summary>
        public bool OpensNearTray { get; set; } = true;

        public SettingsWindow()
        {
            InitializeComponent();
            ShowTab(SettingsTab.Spider);
        }

        /// <summary>Tepsi ikonuna tıklanınca: kapalıysa açar, açıksa kapatır.</summary>
        public void ToggleFromTray()
        {
            if (IsVisible)
            {
                Hide();
            }
            else if (FlyoutToggle.ShouldOpen(IsVisible, _lastAutoHiddenAt, DateTime.UtcNow))
            {
                ShowOrActivate();
            }
        }

        /// <summary>Pencereyi gösterir; açıksa öne getirir, simge durumundaysa geri açar.</summary>
        public void ShowOrActivate()
        {
            if (!IsVisible)
            {
                if (OpensNearTray) PlaceNearTray();
                Show();
                PlayOpenAnimation();
            }
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
        }

        public void ShowTab(SettingsTab tab)
        {
            CurrentTab = tab;
            NavButtonFor(tab).IsChecked = true;

            foreach (SettingsTab each in Enum.GetValues<SettingsTab>())
            {
                PaneFor(each).Visibility = each == tab ? Visibility.Visible : Visibility.Collapsed;
            }

            // Yeni sekmenin içeriği yumuşakça belirir
            PaneFor(tab).BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
        }

        // Kapatma düğmesi pencereyi yok etmez, gizler: içindeki sekmeler denetleyicilere bağlıdır ve tekrar açılınca aynen döner.
        // Uygulama kapanırken (Application.Shutdown) Windows bu iptali dikkate almaz, pencere gerçekten kapanır.
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;
            Hide();
        }

        // Pencerenin dışına tıklanınca gizlen
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            if (!IsVisible) return;

            _lastAutoHiddenAt = DateTime.UtcNow;
            Hide();
        }

        private void PlaceNearTray()
        {
            var screen = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
            var (topLeft, height) = FlyoutPlacement.Place(screen, SystemParameters.WorkArea, FlyoutSize);

            Left = topLeft.X - ShadowMargin;
            Top = topLeft.Y - ShadowMargin;
            Height = height + 2 * ShadowMargin;
        }

        // Windows'un kendi tepsi pencereleri gibi: hafifçe aşağıdan kayarak ve belirerek gelir
        private void PlayOpenAnimation()
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            RootOffset.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty,
                new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(260)) { EasingFunction = ease });
            Root.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e) => Hide();

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Hide();
        }

        private void OnNavChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton { Tag: string name } && Enum.TryParse(name, out SettingsTab tab) && tab != CurrentTab)
            {
                ShowTab(tab);
            }
        }

        private RadioButton NavButtonFor(SettingsTab tab) => tab switch
        {
            SettingsTab.Spider => SpiderNav,
            SettingsTab.Pomodoro => PomodoroNav,
            SettingsTab.Reminders => RemindersNav,
            _ => SettingsNav
        };

        private FrameworkElement PaneFor(SettingsTab tab) => tab switch
        {
            SettingsTab.Spider => SpiderPane,
            SettingsTab.Pomodoro => PomodoroPane,
            SettingsTab.Reminders => RemindersPane,
            _ => SettingsPane
        };
    }
}
