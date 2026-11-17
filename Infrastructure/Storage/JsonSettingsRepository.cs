using SpiderDesk.Core.Settings;

namespace SpiderDesk.Infrastructure.Storage
{
    /// <summary>
    /// Kullanıcı ayarlarını ve yakalanan toplam böcek skorunu settings.json dosyasında kalıcı tutar.
    /// Dosya yoksa ya da bozuksa varsayılan ayarlarla başlanır.
    /// </summary>
    public class JsonSettingsRepository : ISettingsRepository
    {
        private readonly JsonFile<UserSettings> _file;

        public string? LastError => _file.LastError;

        public JsonSettingsRepository()
            : this(AppDataFolder.PathOf("settings.json"))
        {
        }

        /// <summary>Testler gerçek ayar dosyasına dokunmamak için geçici bir dosya yolu verir.</summary>
        public JsonSettingsRepository(string filePath)
        {
            _file = new JsonFile<UserSettings>(filePath);
        }

        public UserSettings Load() => _file.Read() ?? new UserSettings();

        public bool Save(UserSettings settings) => _file.Write(settings);
    }
}
