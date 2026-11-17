using System.IO;
using SpiderDesk.Core.Settings;
using SpiderDesk.Infrastructure.Storage;

namespace SpiderDesk.Tests.Infrastructure.Storage
{
    /// <summary>
    /// Ayar dosyasının, Storage klasörüne taşınıp ortak JsonFile sınıfını kullanmaya başladıktan sonra da
    /// eskisi gibi çalıştığını doğrular.
    /// </summary>
    public class JsonSettingsRepositoryTests : IDisposable
    {
        private readonly TempFolder _folder = new();
        private readonly string _path;

        public JsonSettingsRepositoryTests()
        {
            _path = _folder.File("settings.json");
        }

        public void Dispose() => _folder.Dispose();

        [Fact]
        public void Load_WhenThereIsNoFileYet_GivesDefaultSettings()
        {
            var repository = new JsonSettingsRepository(_path);

            var settings = repository.Load();

            Assert.Equal(new UserSettings().PomodoroPreset, settings.PomodoroPreset);
            Assert.Equal(0, settings.TotalBugsCaught);
            Assert.Null(repository.LastError);
        }

        [Fact]
        public void SavedSettings_ComeBackTheSame_AfterTheAppRestarts()
        {
            var settings = new UserSettings
            {
                PomodoroPreset = PomodoroPreset.Extended,
                SpeedPreset = SpiderSpeedPreset.Agile,
                TotalBugsCaught = 42
            };

            Assert.True(new JsonSettingsRepository(_path).Save(settings));
            var loaded = new JsonSettingsRepository(_path).Load();

            Assert.Equal(PomodoroPreset.Extended, loaded.PomodoroPreset);
            Assert.Equal(SpiderSpeedPreset.Agile, loaded.SpeedPreset);
            Assert.Equal(42, loaded.TotalBugsCaught);
        }

        [Fact]
        public void Load_WhenTheFileIsBroken_GivesDefaultSettings_AndRemembersTheError()
        {
            File.WriteAllText(_path, "{ bu json değil");
            var repository = new JsonSettingsRepository(_path);

            var settings = repository.Load();

            Assert.Equal(0, settings.TotalBugsCaught);
            Assert.NotNull(repository.LastError);
        }
    }
}
