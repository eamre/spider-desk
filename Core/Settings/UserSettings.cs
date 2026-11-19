using SpiderDesk.Core.Services;

namespace SpiderDesk.Core.Settings
{
    public class UserSettings
    {
        public int TotalBugsCaught { get; set; }
        // Uygulamayı ilk kez açan kişi Orman Tarantulası'nı görür (SpiderThemeCatalog.Default ile aynı tema)
        public string ThemeId { get; set; } = "forest_tarantula";
        public BugSpawnRatePreset SpawnRate { get; set; } = BugSpawnRatePreset.Low;
        public DewSpawnRatePreset DewSpawnRate { get; set; } = DewSpawnRatePreset.Normal;
        public SpiderSpeedPreset SpeedPreset { get; set; } = SpiderSpeedPreset.Normal;
        public EnvironmentTimeMode TimeMode { get; set; } = EnvironmentTimeMode.AutoBySystemClock;
        public PomodoroPreset PomodoroPreset { get; set; } = PomodoroPreset.Classic;
        public bool AlwaysOnTop { get; set; }
        public bool RunOnStartup { get; set; } = true;
    }
}
