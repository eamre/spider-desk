using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Forms;
using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Spider.Themes;
using SpiderDesk.Infrastructure.Platform;
using SpiderDesk.Infrastructure.Sessions;
using SpiderDesk.Infrastructure.Tray;
using SpiderDesk.Infrastructure.Windowing;
using SpiderDesk.Rendering;
using SpiderDesk.Rendering.Web;

namespace SpiderDesk.Tests.Infrastructure.Sessions
{
    /// <summary>
    /// Tepsideki ayar menüsünden yapılan her seçimin hem simülasyona uygulandığını hem de diske kaydedildiğini doğrular.
    /// Menü maddelerine gerçekten tıklanır (PerformClick). WPF çizim nesneleri STA iş parçacığı istediği için testler orada çalışır.
    /// </summary>
    public class SettingsSessionControllerTests
    {
        private sealed class FakeSettingsRepository : ISettingsRepository
        {
            public int SaveCount { get; private set; }
            public string? LastError => null;
            public UserSettings Load() => new();
            public bool Save(UserSettings settings)
            {
                SaveCount++;
                return true;
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
            public SettingsMenu Menu { get; }

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
                Menu = new SettingsMenu(Settings.Current, startupEnabled: false, new KeepMenuOpen());

                _ = new SettingsSessionController(Menu, Engine, Settings, canvas, webRenderer, WindowLayer, Startup);
            }

            /// <summary>Menüde (alt menüler dahil) bu yazıyla başlayan maddeye tıklar.</summary>
            public void Click(string text)
            {
                ToolStripMenuItem item = Find(Menu.Items, text) ?? throw new InvalidOperationException($"Menüde '{text}' yok");
                item.PerformClick();
            }

            private static ToolStripMenuItem? Find(IEnumerable<ToolStripItem> items, string text)
            {
                foreach (ToolStripItem item in items)
                {
                    if (item is not ToolStripMenuItem menuItem) continue;
                    if (menuItem.Text != null && menuItem.Text.StartsWith(text, StringComparison.Ordinal)) return menuItem;
                    if (Find(menuItem.DropDownItems.Cast<ToolStripItem>(), text) is { } inner) return inner;
                }
                return null;
            }
        }

        private static void RunOnSta(Action<Rig> test)
        {
            ExceptionDispatchInfo? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    test(new Rig());
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            failure?.Throw();
        }

        [Fact]
        public void ChoosingATheme_DressesTheSpider_AndSavesIt()
        {
            RunOnSta(rig =>
            {
                ISpiderTheme other = SpiderThemeCatalog.All.First(t => t.Id != rig.Settings.Current.ThemeId);

                rig.Click(other.DisplayName);

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
                rig.Click("Yoğun (8-17 sn)");

                Assert.Equal(BugSpawnRatePreset.High, rig.Settings.Current.SpawnRate);
                Assert.Equal(1, rig.Repository.SaveCount);
            });
        }

        [Fact]
        public void ChoosingADewRate_ChangesTheWorld_AndSavesIt()
        {
            RunOnSta(rig =>
            {
                rig.Click("Kapalı");

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
                rig.Click("Çevik (1.35x)");

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
                rig.Click("Sürekli Gece");

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
                rig.Click("🖥️ Pencerelerin Üstünde Göster");

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

                rig.Click("🚀 Bilgisayar Açıldığında Otomatik Başlat");

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
    }
}
