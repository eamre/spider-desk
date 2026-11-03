namespace SpiderDesk.Core
{
    public enum BugSpawnRatePreset
    {
        Low,    // Seyrek (45-75 sn)
        Normal, // Normal (25-45 sn)
        High    // Yoğun (10-20 sn)
    }

    public class UserSettings
    {
        public int TotalBugsCaught { get; set; }
        public string ThemeId { get; set; } = "black_widow";
        public BugSpawnRatePreset SpawnRate { get; set; } = BugSpawnRatePreset.Low;
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
