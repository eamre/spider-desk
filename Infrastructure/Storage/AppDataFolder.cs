using System;
using System.IO;

namespace SpiderDesk.Infrastructure.Storage
{
    /// <summary>
    /// Uygulamanın kendi dosyalarını tuttuğu klasör: %LocalAppData%\SpiderDesk
    /// (örneğin C:\Users\Ad\AppData\Local\SpiderDesk). Ayarlar ve hatırlatmalar burada saklanır.
    /// </summary>
    public static class AppDataFolder
    {
        public static string PathOf(string fileName)
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpiderDesk");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, fileName);
        }
    }
}
