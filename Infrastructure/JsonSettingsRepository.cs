using System;
using System.IO;
using System.Text.Json;
using SpiderDesk.Core;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Kullanıcı ayarlarını ve yakalanan toplam böcek skorunu JSON olarak kalıcı kaydeder (SRP & DIP).
    /// </summary>
    public class JsonSettingsRepository : ISettingsRepository
    {
        private readonly string _filePath;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public JsonSettingsRepository()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "SpiderDesk");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "settings.json");
        }

        public UserSettings Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? new UserSettings();
                }
            }
            catch
            {
                // Bozuk dosya durumunda varsayılan ayarlarla devam et
            }

            return new UserSettings();
        }

        public void Save(UserSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Disk yazma hatalarını sessizce geç
            }
        }
    }
}
