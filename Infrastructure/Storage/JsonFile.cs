using System;
using System.IO;
using System.Text.Json;
using SpiderDesk.Core;

namespace SpiderDesk.Infrastructure.Storage
{
    /// <summary>
    /// Tek bir nesneyi JSON dosyası olarak okur ve yazar. Ayarlar ve hatırlatmalar aynı güvenli yolu kullanır:
    /// <list type="bullet">
    /// <item>Dosya yoksa ya da bozuksa uygulama çökmez; okuma null döner ve sorun tanılama günlüğüne yazılır.</item>
    /// <item>Yazarken önce geçici bir dosyaya yazılır, sonra asıl dosyanın yerine konur. Yazma yarıda kesilirse
    /// (örneğin bilgisayar kapanırsa) eski dosya bozulmadan kalır.</item>
    /// </list>
    /// </summary>
    public sealed class JsonFile<T> where T : class
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public string FilePath { get; }

        /// <summary>Son okuma ya da yazma başarısız olduysa hatanın açıklaması; başarılıysa null.</summary>
        public string? LastError { get; private set; }

        public JsonFile(string filePath)
        {
            FilePath = filePath;
        }

        /// <summary>Dosyayı okur. Dosya yoksa, boşsa ya da okunamıyorsa null döner.</summary>
        public T? Read()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;

                T? loaded = JsonSerializer.Deserialize<T>(File.ReadAllText(FilePath), JsonOptions);
                if (loaded != null)
                {
                    LastError = null;
                }
                return loaded;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AppDiagnostics.LogWarning(nameof(JsonFile<T>), $"Dosya okunamadı ({FilePath}): {ex.Message}");
                return null;
            }
        }

        /// <summary>Nesneyi dosyaya yazar. Başarılıysa true döner.</summary>
        public bool Write(T value)
        {
            try
            {
                string? dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string tempPath = FilePath + ".tmp";
                File.WriteAllText(tempPath, JsonSerializer.Serialize(value, JsonOptions));
                File.Move(tempPath, FilePath, overwrite: true);

                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                AppDiagnostics.LogError(nameof(JsonFile<T>), ex);
                return false;
            }
        }
    }
}
