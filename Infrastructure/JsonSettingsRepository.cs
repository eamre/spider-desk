using System;
using System.IO;
using System.Text.Json;
using SpiderDesk.Core;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Kullanıcı ayarlarını ve yakalanan toplam böcek skorunu JSON olarak kalıcı kaydeder.
    /// </summary>
    public class JsonSettingsRepository : ISettingsRepository
    {
        private readonly string _filePath;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public string? LastError { get; private set; }

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
                    var loaded = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
                    if (loaded != null)
                    {
                        LastError = null;
                        return loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AppDiagnostics.LogWarning(nameof(JsonSettingsRepository), $"Ayar dosyası okunamadı ({_filePath}): {ex.Message}");
            }

            return new UserSettings();
        }

        public bool Save(UserSettings settings)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string tempPath = _filePath + ".tmp";
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _filePath, overwrite: true);

                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AppDiagnostics.LogError(nameof(JsonSettingsRepository), ex);
                return false;
            }
        }
    }
}
