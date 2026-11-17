using SpiderDesk.Core.Settings;

namespace SpiderDesk.Tests.Core.Settings
{
    public class SettingsStoreTests
    {
        /// <summary>
        /// Gerçek dosyaya yazmayan sahte depo: ne zaman yüklendiğini ve kaç kez kaydedildiğini sayar.
        /// SettingsStore yalnızca ISettingsRepository'yi tanıdığı için gerçek deponun yerine bunu verebiliriz.
        /// </summary>
        private sealed class FakeSettingsRepository : ISettingsRepository
        {
            public UserSettings Stored { get; } = new() { ThemeId = "frost_crystal" };
            public int LoadCount { get; private set; }
            public List<UserSettings> Saves { get; } = new();

            public string? LastError => null;

            public UserSettings Load()
            {
                LoadCount++;
                return Stored;
            }

            public bool Save(UserSettings settings)
            {
                Saves.Add(settings);
                return true;
            }
        }

        [Fact]
        public void Constructor_LoadsSettingsOnce_AndDoesNotSave()
        {
            var repository = new FakeSettingsRepository();

            var store = new SettingsStore(repository);

            Assert.Equal(1, repository.LoadCount);
            Assert.Same(repository.Stored, store.Current);
            Assert.Empty(repository.Saves);
        }

        [Fact]
        public void Update_AppliesTheChange_AndSavesIt()
        {
            var repository = new FakeSettingsRepository();
            var store = new SettingsStore(repository);

            store.Update(s => s.SpeedPreset = SpiderSpeedPreset.Agile);

            Assert.Equal(SpiderSpeedPreset.Agile, store.Current.SpeedPreset);
            Assert.Single(repository.Saves);
            Assert.Same(store.Current, repository.Saves[0]);
        }

        [Fact]
        public void EveryUpdate_IsSaved_AndEarlierChangesAreKept()
        {
            var repository = new FakeSettingsRepository();
            var store = new SettingsStore(repository);

            store.Update(s => s.SpawnRate = BugSpawnRatePreset.High);
            store.Update(s => s.AlwaysOnTop = true);

            Assert.Equal(2, repository.Saves.Count);
            Assert.Equal(BugSpawnRatePreset.High, store.Current.SpawnRate);
            Assert.True(store.Current.AlwaysOnTop);
            Assert.Equal("frost_crystal", store.Current.ThemeId);
            Assert.Equal(1, repository.LoadCount);
        }
    }
}
