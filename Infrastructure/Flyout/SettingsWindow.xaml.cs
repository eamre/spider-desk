using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SpiderDesk.Infrastructure.Flyout
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

        // Açılırken pencere bu kadar aşağıdan yukarı kayarak gelir
        private const double OpenSlideDistance = 18;

        // Kapanırken içeriğin sönme süresi; pencere bu süre bitince gizlenir
        private static readonly TimeSpan CloseFadeDuration = TimeSpan.FromMilliseconds(120);

        private DateTime? _lastAutoHiddenAt;

        // Kapanış sırasında çalışan sönme animasyonu; null ise pencere kapanmıyor demektir
        private DoubleAnimation? _closingFade;

        /// <summary>Pencere ekranda ve kapanmakta değil (sönerken artık açık sayılmaz).</summary>
        private bool IsOpen => IsVisible && _closingFade == null;

        public SettingsTab CurrentTab { get; private set; }

        /// <summary>"Örümcek" sekmesi; <see cref="Sessions.SpiderSessionController"/> buna bağlanır.</summary>
        public Pages.SpiderPage SpiderPage => SpiderPageView;

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
            if (IsOpen)
            {
                FadeOutAndHide();
            }
            else if (FlyoutToggle.ShouldOpen(IsOpen, _lastAutoHiddenAt, DateTime.UtcNow))
            {
                ShowOrActivate();
            }
        }

        /// <summary>Pencereyi gösterir; açıksa öne getirir, simge durumundaysa geri açar. Sönerken çağrılırsa sönmeyi iptal eder.</summary>
        public void ShowOrActivate()
        {
            if (!IsOpen)
            {
                _closingFade = null;
                if (!IsVisible)
                {
                    if (OpensNearTray) PlaceNearTray();
                    HideContentBeforeOpening();
                    Show();
                }
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
            FadeOutAndHide();
        }

        // Pencerenin dışına tıklanınca gizlen
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            if (!IsOpen) return;

            _lastAutoHiddenAt = DateTime.UtcNow;
            FadeOutAndHide();
        }

        // Gizlenen pencere, Windows'ta son çizilen karesini saklar ve yeniden gösterilince bir an o kareyi gösterir.
        // Pencere bir anda gizlenirse bu kare tam görünür olur ve açılış titrer. Bu yüzden önce içerik söndürülür,
        // pencere ancak son kareler görünmez çizildikten sonra gizlenir.
        private void FadeOutAndHide()
        {
            if (!IsOpen) return;

            // Sonu yavaşlayan sönme: son kareler neredeyse tamamen saydam çizilir
            var fade = new DoubleAnimation(0, CloseFadeDuration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            _closingFade = fade;
            fade.Completed += (_, _) =>
            {
                // Bitişteki saydam kare çizilsin diye gizleme, çizim işinden sonraya bırakılır
                Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    // Bu arada pencere yeniden açıldıysa gizlenmez
                    if (_closingFade != fade) return;
                    _closingFade = null;
                    Hide();
                });
            };
            Root.BeginAnimation(OpacityProperty, fade);
        }

        private void PlaceNearTray()
        {
            var screen = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
            var (topLeft, height) = FlyoutPlacement.Place(screen, SystemParameters.WorkArea, FlyoutSize);

            Left = topLeft.X - ShadowMargin;
            Top = topLeft.Y - ShadowMargin;
            Height = height + 2 * ShadowMargin;
        }

        // Windows pencereyi ilk kez gösterirken hemen çizer. İçerik o anda görünür olursa ilk kare tam görünür çizilir,
        // sonra açılış animasyonu onu görünmez hâle çekip yeniden getirir: pencere bir an kaybolmuş gibi olur.
        // Bu yüzden içerik, animasyonun başlayacağı hâle pencere gösterilmeden önce getirilir.
        private void HideContentBeforeOpening()
        {
            // Önceki açılışın animasyonu son değerini (tam görünür) tutuyor; önce onu kaldırır
            Root.BeginAnimation(OpacityProperty, null);
            RootOffset.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);

            Root.Opacity = 0;
            RootOffset.Y = OpenSlideDistance;
        }

        // Windows'un kendi tepsi pencereleri gibi: hafifçe aşağıdan kayarak ve belirerek gelir.
        // Başlangıç değeri verilmez: animasyon içeriğin o anki hâlinden başlar. Yeni açılışta bu görünmez ve aşağıda
        // bekleyen hâldir; sönerken yeniden açılınca da yarı sönük hâlden geri gelir, önce bir anda kaybolmaz.
        private void PlayOpenAnimation()
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            RootOffset.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty,
                new DoubleAnimation(0, TimeSpan.FromMilliseconds(260)) { EasingFunction = ease });
            Root.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(180)));
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e) => FadeOutAndHide();

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) FadeOutAndHide();
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
