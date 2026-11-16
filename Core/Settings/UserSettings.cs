using SpiderDesk.Core.Services;

namespace SpiderDesk.Core.Settings
{
    public class UserSettings
    {
        public int TotalBugsCaught { get; set; }
        public string ThemeId { get; set; } = "black_widow";
        public BugSpawnRatePreset SpawnRate { get; set; } = BugSpawnRatePreset.Low;
        public DewSpawnRatePreset DewSpawnRate { get; set; } = DewSpawnRatePreset.Normal;
        public SpiderSpeedPreset SpeedPreset { get; set; } = SpiderSpeedPreset.Normal;
        public EnvironmentTimeMode TimeMode { get; set; } = EnvironmentTimeMode.AutoBySystemClock;
        public bool AlwaysOnTop { get; set; }
        public bool RunOnStartup { get; set; } = true;
    }
}
