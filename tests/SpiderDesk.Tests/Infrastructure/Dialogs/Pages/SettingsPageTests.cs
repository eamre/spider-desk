using System.Windows.Controls;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Spider.Themes;
using SpiderDesk.Infrastructure.Dialogs.Pages;

namespace SpiderDesk.Tests.Infrastructure.Dialogs.Pages
{
    /// <summary>
    /// "Ayarlar" sekmesinin kendisini doğrular: ayarları doğru gösterdiğini, gösterirken olay çıkarmadığını
    /// ve kullanıcının seçimini doğru değerle bildirdiğini.
    /// </summary>
    public class SettingsPageTests
    {
        private static RadioButton Checked(SettingsPage page, string panelName) =>
            ((Panel)page.FindName(panelName)).Children.OfType<RadioButton>().Single(b => b.IsChecked == true);

        private static RadioButton Button(SettingsPage page, string panelName, string label) =>
            ((Panel)page.FindName(panelName)).Children.OfType<RadioButton>()
                .Single(b => (b.Content as string ?? b.ToolTip as string) == label);

        [Fact]
        public void ShowsEveryTheme_WithoutTheEmojiInItsName()
        {
            StaThread.Run(() =>
            {
                var page = new SettingsPage();
                var labels = ((Panel)page.FindName("ThemeChoices")).Children.OfType<RadioButton>().Select(b => (string)b.ToolTip).ToList();

                Assert.Equal(SpiderThemeCatalog.All.Count, labels.Count);
                Assert.Contains("Karadul (Klasik)", labels);
                Assert.All(labels, label => Assert.True(char.IsLetter(label[0]), label));
            });
        }

        [Fact]
        public void ShowSettings_MarksTheCurrentChoices()
        {
            StaThread.Run(() =>
            {
                var page = new SettingsPage();
                var settings = new UserSettings
                {
                    ThemeId = "cyber_neon",
                    TimeMode = EnvironmentTimeMode.AlwaysNight,
                    SpeedPreset = SpiderSpeedPreset.Calm,
                    SpawnRate = BugSpawnRatePreset.High,
                    DewSpawnRate = DewSpawnRatePreset.Off,
                    AlwaysOnTop = true
                };

                page.ShowSettings(settings, runsOnStartup: true);

                Assert.Equal("Siber Neon", Checked(page, "ThemeChoices").ToolTip);
                Assert.Equal("Gece", Checked(page, "TimeModeChoices").Content);
                Assert.Equal("Sakin", Checked(page, "SpeedChoices").Content);
                Assert.Equal("Yoğun", Checked(page, "SpawnRateChoices").Content);
                Assert.Equal("Kapalı", Checked(page, "DewChoices").Content);
                Assert.True(((CheckBox)page.FindName("AlwaysOnTopSwitch")).IsChecked);
                Assert.True(((CheckBox)page.FindName("StartupSwitch")).IsChecked);
            });
        }

        [Fact]
        public void ShowSettings_DoesNotReportAnyChoice()
        {
            StaThread.Run(() =>
            {
                var page = new SettingsPage();
                int reported = 0;
                page.ThemeSelected += _ => reported++;
                page.TimeModeSelected += _ => reported++;
                page.SpeedPresetSelected += _ => reported++;
                page.SpawnRateSelected += _ => reported++;
                page.DewSpawnRateSelected += _ => reported++;
                page.AlwaysOnTopChanged += _ => reported++;
                page.StartupChanged += _ => reported++;

                page.ShowSettings(new UserSettings { AlwaysOnTop = true, ThemeId = "frost_crystal" }, runsOnStartup: true);

                Assert.Equal(0, reported);
            });
        }

        [Fact]
        public void ChoosingAnOption_ReportsItsValue()
        {
            StaThread.Run(() =>
            {
                var page = new SettingsPage();
                page.ShowSettings(new UserSettings(), runsOnStartup: false);
                DewSpawnRatePreset? dew = null;
                bool? startup = null;
                page.DewSpawnRateSelected += rate => dew = rate;
                page.StartupChanged += on => startup = on;

                Button(page, "DewChoices", "Yoğun").IsChecked = true;
                ((CheckBox)page.FindName("StartupSwitch")).IsChecked = true;

                Assert.Equal(DewSpawnRatePreset.High, dew);
                Assert.True(startup);
            });
        }

        [Fact]
        public void TurningASwitchOff_ReportsFalse()
        {
            StaThread.Run(() =>
            {
                var page = new SettingsPage();
                page.ShowSettings(new UserSettings { AlwaysOnTop = true }, runsOnStartup: false);
                bool? alwaysOnTop = null;
                page.AlwaysOnTopChanged += on => alwaysOnTop = on;

                ((CheckBox)page.FindName("AlwaysOnTopSwitch")).IsChecked = false;

                Assert.False(alwaysOnTop);
            });
        }
    }
}
