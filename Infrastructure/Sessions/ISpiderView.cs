using System;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Infrastructure.Sessions
{
    /// <summary>
    /// Örümceğin durumunun gösterildiği ve ona komut verildiği yer (ayarlar penceresinin "Örümcek" sekmesi).
    /// <see cref="SpiderSessionController"/> yalnızca bu arayüzü tanır; testlerde yerine sahte bir görünüm verilir.
    /// </summary>
    public interface ISpiderView
    {
        event Action? NewWebRequested;
        event Action? RappelRequested;
        event Action? RainRequested;
        event Action? SpawnBugsRequested;
        event Action? ClearWebsRequested;

        /// <summary>Seviyeyi, tokluğu, toplam avı ve örümceğin şu an ne yaptığını gösterir.</summary>
        void ShowStatus(SpiderStatusDisplay status);
    }
}
