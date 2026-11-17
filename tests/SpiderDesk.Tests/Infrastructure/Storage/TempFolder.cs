using System.IO;

namespace SpiderDesk.Tests.Infrastructure.Storage
{
    /// <summary>
    /// Her test için boş, geçici bir klasör açar ve test bitince siler.
    /// Böylece testler kullanıcının gerçek settings.json / reminders.json dosyalarına hiç dokunmaz.
    /// </summary>
    public sealed class TempFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SpiderDeskTests", Guid.NewGuid().ToString("N"));

        public TempFolder()
        {
            Directory.CreateDirectory(Path);
        }

        public string File(string name) => System.IO.Path.Combine(Path, name);

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Geçici klasör silinemezse testi başarısız saymaya gerek yok
            }
        }
    }
}
