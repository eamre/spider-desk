namespace SpiderDesk.Core
{
    public class UserSettings
    {
        public int TotalBugsCaught { get; set; }
        public string ThemeId { get; set; } = "black_widow";
        public BugSpawnRatePreset SpawnRate { get; set; } = BugSpawnRatePreset.Low;
        public DewSpawnRatePreset DewSpawnRate { get; set; } = DewSpawnRatePreset.Normal;
        public double SpeedMultiplier { get; set; } = 1.0;
        public EnvironmentTimeMode TimeMode { get; set; } = EnvironmentTimeMode.AutoBySystemClock;
        public bool AlwaysOnTop { get; set; }
        public bool RunOnStartup { get; set; } = true;
    }
}
