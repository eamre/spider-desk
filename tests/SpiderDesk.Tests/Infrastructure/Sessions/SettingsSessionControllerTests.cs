using System.Windows;
using System.Windows.Controls;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Spider.Themes;
using SpiderDesk.Infrastructure.Dialogs.Pages;
using SpiderDesk.Infrastructure.Platform;
using SpiderDesk.Infrastructure.Sessions;
using SpiderDesk.Infrastructure.Windowing;
using SpiderDesk.Rendering;
using SpiderDesk.Rendering.Web;

namespace SpiderDesk.Tests.Infrastructure.Sessions
{
    /// <summary>
    /// Ayarlar sekmesinde yapılan her seçimin hem simülasyona uygulandığını hem de diske kaydedildiğini doğrular.
    /// Gerçek <see cref="SettingsPage"/> kullanılır ve düğmeleri kullanıcı gibi seçilir. WPF nesneleri STA iş parçacığı istediği için
    /// testler orada çalışır.
    /// </summary>
    public class SettingsSessionControllerTests
    {
        private sealed class FakeSettingsRepository : ISettingsRepository
        {
            public int SaveCount { get; private set; }
            public bool FailSaves { get; set; }
            public string? LastError => null;
            public UserSettings Load() => new();
            public bool Save(UserSettings settings)
            {
                SaveCount++;
                return !FailSaves;
            }
        }

        private sealed class FakeWindowLayer : IWindowLayerService
        {
            public bool AlwaysOnTop { get; set; }
            public bool IsPresentingOnTop { get; set; }
            public bool IsOverlayInteractive => true;
            public bool IsDesktopOccluded => false;
            public bool IsDesktopCovered => false;
            public int EnforceCount { get; private set; }
            public void Attach(Window window) { }
            public void EnforceZOrder() => EnforceCount++;
        }

        private sealed class FakeStartup : IStartupRegistrationService
        {
            public bool? LastSet { get; private set; }
            public bool IsEnabled() => LastSet ?? false;
            public void SetEnabled(bool enable) => LastSet = enable;
        }

        private sealed class FakeEnvironment : IEnvironmentService
        {
            public EnvironmentTimeMode TimeMode { get; set; }
            public bool IsNightTime => false;
            public TimeSpan LocalTimeOfDay => TimeSpan.FromHours(10);
        }

        private sealed class Rig
        {
            public FakeSettingsRepository Repository { get; } = new();
            public FakeWindowLayer WindowLayer { get; } = new();
            public FakeStartup Startup { get; } = new();
            public FakeEnvironment Environment { get; } = new();
            public SettingsStore Settings { get; }
            public SimulationEngine Engine { get; }
            public SettingsPage Page { get; } = new();

            public Rig()
            {
                Settings = new SettingsStore(Repository);
                Engine = new SimulationEngine(new BugSpeciesCatalog(), Environment, Settings.Current, 1920, 1080);
                var webRenderer = new WebRenderer();
                var canvas = new SimulationCanvas(
                    Engine.World,
                    Engine.Spider,
                    webRenderer,
                    new BugRenderer(),
                    new SpiderRenderer(new NoteRenderer()),
                    new SilkWritingRenderer(() => Engine.Spider.Theme));

                _ = new SettingsSessionController(Page, Engine, Settings, canvas, webRenderer, WindowLayer, Startup);
            }

            /// <summary>Sekmede bu yazıyı taşıyan düğmeyi seçer (kullanıcı tıklamış gibi).</summary>
            public void Choose(string panelName, string label)
            {
                var panel = (Panel)Page.FindName(panelName);
                RadioButton button = panel.Children.OfType<RadioButton>()
                    .Single(b => (b.Content as string ?? b.ToolTip as string) == label);
                button.IsChecked = true;
            }

            public void Toggle(string switchName)
            {
                var toggle = (CheckBox)Page.FindName(switchName);
                toggle.IsChecked = toggle.IsChecked != true;
            }

            public bool IsWarningVisible => ((FrameworkElement)Page.FindName("SaveWarning")).Visibility == Visibility.Visible;
        }

        private static void RunOnSta(Action<Rig> test) => StaThread.Run(() => test(new Rig()));

        [Fact]
        public void ChoosingATheme_DressesTheSpider_AndSavesIt()
        {
            RunOnSta(rig =>
            {
                ISpiderTheme other = SpiderThemeCatalog.All.First(t => t.Id != rig.Settings.Current.ThemeId);
                string label = new string(other.DisplayName.SkipWhile(c => !char.IsLetter(c)).ToArray());

                rig.Choose("ThemeChoices", label);

                Assert.Equal(other.Id, rig.Engine.Spider.Theme.Id);
                Assert.Equal(other.Id, rig.Settings.Current.ThemeId);
                Assert.Equal(1, rig.Repository.SaveCount);
            });
        }

        [Fact]
        public void ChoosingABugSpawnRate_SavesIt()
        {
            RunOnSta(rig =>
            {
                rig.Choose("SpawnRateChoices", "Yoğun");

                Assert.Equal(BugSpawnRatePreset.High, rig.Settings.Current.SpawnRate);
                Assert.Equal(1, rig.Repository.SaveCount);
            });
        }

        [Fact]
        public void ChoosingADewRate_ChangesTheWorld_AndSavesIt()
        {
            RunOnSta(rig =>
            {
                rig.Choose("DewChoices", "Kapalı");

                Assert.Equal(DewSpawnRatePreset.Off, rig.Engine.World.DewSpawnRate);
                Assert.Equal(DewSpawnRatePreset.Off, rig.Settings.Current.DewSpawnRate);
                Assert.Equal(1, rig.Repository.SaveCount);
            });
        }

        [Fact]
        public void ChoosingASpeed_ChangesHowFastTheSpiderMoves_AndSavesIt()
        {
            RunOnSta(rig =>
            {
                rig.Choose("SpeedChoices", "Çevik");

                Assert.Equal(SpiderSpeedPolicy.MultiplierFor(SpiderSpeedPreset.Agile), rig.Engine.Spider.SpeedMultiplier);
                Assert.Equal(SpiderSpeedPreset.Agile, rig.Settings.Current.SpeedPreset);
                Assert.Equal(1, rig.Repository.SaveCount);
            });
        }

        [Fact]
        public void ChoosingADayNightMode_ChangesTheEnvironment_AndSavesIt()
        {
            RunOnSta(rig =>
            {
                rig.Choose("TimeModeChoices", "Gece");

                Assert.Equal(EnvironmentTimeMode.AlwaysNight, rig.Environment.TimeMode);
                Assert.Equal(EnvironmentTimeMode.AlwaysNight, rig.Settings.Current.TimeMode);
                Assert.Equal(1, rig.Repository.SaveCount);
            });
        }

        [Fact]
        public void TurningOnAlwaysOnTop_RaisesTheWindowAtOnce_AndSavesIt()
        {
            RunOnSta(rig =>
            {
                rig.Toggle("AlwaysOnTopSwitch");

                Assert.True(rig.WindowLayer.AlwaysOnTop);
                Assert.Equal(1, rig.WindowLayer.EnforceCount);
                Assert.True(rig.Settings.Current.AlwaysOnTop);
                Assert.Equal(1, rig.Repository.SaveCount);
            });
        }

        [Fact]
        public void TurningOnRunAtStartup_RegistersWithWindows_AndSavesIt()
        {
            RunOnSta(rig =>
            {
                rig.Settings.Current.RunOnStartup = false;

                rig.Toggle("StartupSwitch");

                Assert.True(rig.Startup.LastSet);
                Assert.True(rig.Settings.Current.RunOnStartup);
                Assert.Equal(1, rig.Repository.SaveCount);
            });
        }

        [Fact]
        public void CatchingPrey_SavesTheTotal()
        {
            RunOnSta(rig =>
            {
                rig.Engine.Spider.RecordPreyCaught();
                rig.Engine.Spider.RecordPreyCaught();

                Assert.Equal(2, rig.Settings.Current.TotalBugsCaught);
                Assert.Equal(2, rig.Repository.SaveCount);
            });
        }

        [Fact]
        public void OnOpening_ThePageShowsTheCurrentSettings_WithoutSavingAnything()
        {
            RunOnSta(rig =>
            {
                var speed = (Panel)rig.Page.FindName("SpeedChoices");
                RadioButton chosen = speed.Children.OfType<RadioButton>().Single(b => b.IsChecked == true);

                Assert.Equal("Normal", chosen.Content);
                Assert.Equal(0, rig.Repository.SaveCount);
            });
        }

        [Fact]
        public void WhenSavingFails_AWarningAppears_AndDisappearsAfterTheNextGoodSave()
        {
            RunOnSta(rig =>
            {
                Assert.False(rig.IsWarningVisible);

                rig.Repository.FailSaves = true;
                rig.Choose("SpeedChoices", "Çevik");
                Assert.True(rig.IsWarningVisible);

                rig.Repository.FailSaves = false;
                rig.Choose("SpeedChoices", "Sakin");
                Assert.False(rig.IsWarningVisible);
            });
        }
    }
}
