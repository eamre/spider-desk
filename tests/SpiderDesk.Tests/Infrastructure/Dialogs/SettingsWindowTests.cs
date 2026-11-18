using System.Windows;
using System.Windows.Controls;
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

                window.ShowOrActivate();

                Assert.Equal(SettingsTab.Pomodoro, window.CurrentTab);
            });
        }
    }
}
