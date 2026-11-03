namespace SpiderDesk.Core
{
    public enum BugSpawnRatePreset
    {
        Low,    // Seyrek (45-75 sn)
        Normal, // Normal (20-38 sn)
        High    // Yoğun (8-17 sn)
    }

    public enum DewSpawnRatePreset
    {
        Off,    // Kapalı
        Low,    // Seyrek (30-50 sn)
        Normal, // Normal (14-26 sn)
        High    // Yoğun (5-10 sn)
    }

    public class UserSettings
    {
        public int TotalBugsCaught { get; set; }
        public string ThemeId { get; set; } = "black_widow";
        public BugSpawnRatePreset SpawnRate { get; set; } = BugSpawnRatePreset.Low;
        public DewSpawnRatePreset DewSpawnRate { get; set; } = DewSpawnRatePreset.Normal;
        public double SpeedMultiplier { get; set; } = 1.0;
        public EnvironmentTimeMode TimeMode { get; set; } = EnvironmentTimeMode.AutoBySystemClock;
        public bool AlwaysOnTop { get; set; }
    }

    public interface ISettingsRepository
    {
        UserSettings Load();
        void Save(UserSettings settings);
    }
}
