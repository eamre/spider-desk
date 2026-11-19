using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using SpiderDesk.Infrastructure.Dialogs;

namespace SpiderDesk.Tests.Infrastructure.Dialogs
{
    /// <summary>
    /// Ayarlar penceresinin iskeletini doğrular: sekmeler arasında geçiş, kapatınca gizlenmesi ve yeniden açılması.
    /// Pencere ekranın görünmeyen bir köşesinde açılır ve odak çalmaz; testi çalıştıran kişi onu görmez.
    /// </summary>
    public class SettingsWindowTests
    {
        private static SettingsWindow OffscreenWindow() => new()
        {
            OpensNearTray = false,
            Left = -20000,
            Top = -20000,
            ShowActivated = false
        };

        private static void WithWindow(Action<SettingsWindow> test) => StaThread.Run(() =>
        {
            SettingsWindow window = OffscreenWindow();
            try
            {
                test(window);
            }
            finally
            {
                // Testin sonunda pencere gerçekten yok edilir (kapatma düğmesi yalnızca gizler)
                window.Dispatcher.InvokeShutdown();
            }
        });

        // Pencerenin çizim ve animasyon işleri yürüsün diye verilen süre boyunca mesajları işler
        private static void WaitWhileTheWindowRuns(TimeSpan duration)
        {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer { Interval = duration };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                frame.Continue = false;
            };
            timer.Start();
            Dispatcher.PushFrame(frame);
        }

        // Kapanış önce içeriği söndürür, sonra pencereyi gizler; pencere gizlenene kadar (en fazla bir saniye) bekler
        private static void WaitUntilHidden(SettingsWindow window)
        {
            for (int i = 0; i < 20 && window.IsVisible; i++) WaitWhileTheWindowRuns(TimeSpan.FromMilliseconds(50));
        }

        private static UIElement Content(SettingsWindow window) => (UIElement)window.FindName("Root");

        private static FrameworkElement Pane(SettingsWindow window, string name) => (FrameworkElement)window.FindName(name);

        private static string[] VisiblePanes(SettingsWindow window) =>
            new[] { "SpiderPane", "PomodoroPane", "RemindersPane", "SettingsPane" }
                .Where(name => Pane(window, name).Visibility == Visibility.Visible)
                .ToArray();

        [Fact]
        public void OpensOnTheSpiderTab()
        {
            WithWindow(window =>
            {
                Assert.Equal(SettingsTab.Spider, window.CurrentTab);
                Assert.Equal(new[] { "SpiderPane" }, VisiblePanes(window));
            });
        }

        [Theory]
        [InlineData(SettingsTab.Pomodoro, "PomodoroPane", "PomodoroNav")]
        [InlineData(SettingsTab.Reminders, "RemindersPane", "RemindersNav")]
        [InlineData(SettingsTab.Settings, "SettingsPane", "SettingsNav")]
        [InlineData(SettingsTab.Spider, "SpiderPane", "SpiderNav")]
        public void ShowingATab_ShowsOnlyThatTab_AndMarksItInTheSidebar(SettingsTab tab, string pane, string nav)
        {
            WithWindow(window =>
            {
                window.ShowTab(SettingsTab.Settings);
                window.ShowTab(tab);

                Assert.Equal(new[] { pane }, VisiblePanes(window));
                Assert.True(((RadioButton)window.FindName(nav)).IsChecked);
            });
        }

        [Fact]
        public void ClickingATabInTheSidebar_ShowsIt()
        {
            WithWindow(window =>
            {
                ((RadioButton)window.FindName("RemindersNav")).IsChecked = true;

                Assert.Equal(SettingsTab.Reminders, window.CurrentTab);
                Assert.Equal(new[] { "RemindersPane" }, VisiblePanes(window));
            });
        }

        [Fact]
        public void Closing_OnlyHidesTheWindow_SoItCanBeOpenedAgain()
        {
            WithWindow(window =>
            {
                window.ShowOrActivate();
                Assert.True(window.IsVisible);

                window.Close();
                WaitUntilHidden(window);
                Assert.False(window.IsVisible);

                // Gerçekten kapansaydı burada "kapanmış pencere yeniden gösterilemez" hatası verirdi
                window.ShowOrActivate();
                Assert.True(window.IsVisible);
            });
        }

        [Fact]
        public void OpeningAMinimizedWindow_RestoresIt()
        {
            WithWindow(window =>
            {
                window.ShowOrActivate();
                window.WindowState = WindowState.Minimized;

                window.ShowOrActivate();

                Assert.Equal(WindowState.Normal, window.WindowState);
            });
        }

        [Fact]
        public void ClickingTheTrayIcon_OpensTheWindow_AndClickingAgainClosesIt()
        {
            WithWindow(window =>
            {
                window.ToggleFromTray();
                Assert.True(window.IsVisible);

                window.ToggleFromTray();
                WaitUntilHidden(window);
                Assert.False(window.IsVisible);
            });
        }

        [Fact]
        public void ChoosingSettingsFromTheTrayMenu_KeepsAnOpenWindowOpen()
        {
            WithWindow(window =>
            {
                window.ShowOrActivate();
                window.ShowOrActivate();

                Assert.True(window.IsVisible);
            });
        }

        [Fact]
        public void ReopeningAfterClosing_KeepsTheLastTab()
        {
            WithWindow(window =>
            {
                window.ShowOrActivate();
                window.ShowTab(SettingsTab.Pomodoro);
                window.Close();
                WaitUntilHidden(window);

                window.ShowOrActivate();

                Assert.Equal(SettingsTab.Pomodoro, window.CurrentTab);
            });
        }

        // Windows pencereyi gösterirken hemen çizer. O anda içerik görünür olursa açılış animasyonu onu
        // görünmez hâle çekip yeniden getirir: pencere açılır, bir an kaybolur, sonra tekrar belirir.
        [Fact]
        public void WhenTheWindowAppears_ItsContentIsStillInvisible_SoTheOpeningDoesNotFlicker()
        {
            WithWindow(window =>
            {
                UIElement root = Content(window);
                var opacityWhenShown = new List<double>();
                window.IsVisibleChanged += (_, e) =>
                {
                    if ((bool)e.NewValue) opacityWhenShown.Add(root.Opacity);
                };

                window.ShowOrActivate();
                // İlk açılışın animasyonu bitsin: içerik tam görünür olur
                WaitWhileTheWindowRuns(TimeSpan.FromMilliseconds(500));
                Assert.Equal(1.0, root.Opacity);

                window.Close();
                WaitUntilHidden(window);
                window.ShowOrActivate();

                Assert.Equal(new[] { 0.0, 0.0 }, opacityWhenShown);
            });
        }

        // Gizlenen pencere son çizilen karesini saklar ve yeniden gösterilince bir an onu gösterir.
        // Bu son kare görünmez olsun diye pencere kapanırken önce içeriği söndürür, sonra gizlenir.
        [Fact]
        public void Closing_FadesTheContentOutBeforeHiding_SoTheNextOpeningDoesNotFlashTheOldImage()
        {
            WithWindow(window =>
            {
                UIElement root = Content(window);
                window.ShowOrActivate();
                WaitWhileTheWindowRuns(TimeSpan.FromMilliseconds(500));

                double? opacityWhenHidden = null;
                window.IsVisibleChanged += (_, e) =>
                {
                    if (!(bool)e.NewValue) opacityWhenHidden = root.Opacity;
                };

                window.Close();
                Assert.True(window.IsVisible); // önce söner, hemen gizlenmez
                WaitUntilHidden(window);

                Assert.False(window.IsVisible);
                Assert.Equal(0.0, opacityWhenHidden);
            });
        }

        [Fact]
        public void OpeningAgainWhileFadingOut_KeepsTheWindowOpen()
        {
            WithWindow(window =>
            {
                window.ShowOrActivate();
                WaitWhileTheWindowRuns(TimeSpan.FromMilliseconds(500));

                window.Close();
                window.ShowOrActivate();
                WaitWhileTheWindowRuns(TimeSpan.FromMilliseconds(500));

                Assert.True(window.IsVisible);
                Assert.Equal(1.0, Content(window).Opacity);
            });
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // Testteki pencere odak almaz, bu yüzden "dışına tıklandı" bilgisi kendiliğinden gelmez.
        // Windows'un gönderdiği mesajların aynısını gönderir: önce "odak sende", sonra "odağı kaybettin".
        private static void ClickOutside(SettingsWindow window)
        {
            const uint WM_ACTIVATE = 0x0006;
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            SendMessage(hwnd, WM_ACTIVATE, new IntPtr(1), IntPtr.Zero);
            SendMessage(hwnd, WM_ACTIVATE, IntPtr.Zero, IntPtr.Zero);
        }

        [Fact]
        public void ClickingOutside_ClosesTheWindow()
        {
            WithWindow(window =>
            {
                window.ShowOrActivate();
                WaitWhileTheWindowRuns(TimeSpan.FromMilliseconds(500));

                ClickOutside(window);
                WaitUntilHidden(window);

                Assert.False(window.IsVisible);
            });
        }

        [Fact]
        public void ClickingTheTrayIconToCloseTheWindow_DoesNotReopenItWhileItFades()
        {
            WithWindow(window =>
            {
                window.ShowOrActivate();
                WaitWhileTheWindowRuns(TimeSpan.FromMilliseconds(500));

                // Tepsi ikonuna tıklamak önce pencerenin dışına tıklamaktır: pencere sönmeye başlar,
                // hemen ardından ikonun tıklaması gelir. Bu tıklama pencereyi kapatmalı, yeniden açmamalı.
                ClickOutside(window);
                window.ToggleFromTray();
                WaitUntilHidden(window);

                Assert.False(window.IsVisible);
            });
        }
    }
}
