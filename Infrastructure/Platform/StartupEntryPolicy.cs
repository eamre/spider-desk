using System;

namespace SpiderDesk.Infrastructure.Platform
{
    /// <summary>Başlangıç kaydı için yapılacak iş.</summary>
    public enum StartupEntryAction
    {
        /// <summary>Kayıt zaten olması gerektiği gibi; dokunma.</summary>
        None,

        /// <summary>Kaydı bu exe'nin yoluyla yaz (yoksa ekle, başka bir exe'yi gösteriyorsa düzelt).</summary>
        Write,

        /// <summary>Kaydı sil.</summary>
        Delete
    }

    /// <summary>
    /// Windows'un "açılışta çalıştır" listesindeki SpiderDesk kaydının ne yapılması gerektiğine karar veren saf kural sınıfı.
    /// Kaydın yalnızca var olması yetmez, <b>çalışan exe'yi</b> göstermesi gerekir: exe taşınırsa (yeni sürüm, başka klasör)
    /// eski kayıt Windows açılışında eski ya da silinmiş bir exe'yi çalıştırmaya çalışırdı.
    /// Kayıt defterine dokunmaz; testlerde doğrudan kullanılır.
    /// </summary>
    public static class StartupEntryPolicy
    {
        /// <summary>Kayda yazılacak değer. Yol tırnak içindedir; böylece boşluk içeren klasör adları da çalışır.</summary>
        public static string EntryFor(string exePath) => $"\"{exePath}\"";

        /// <summary>
        /// Kayıt bu exe'yi mi gösteriyor? Windows'ta dosya yollarında büyük/küçük harf fark etmez,
        /// kayıt tırnaklı ya da tırnaksız yazılmış olabilir.
        /// </summary>
        public static bool PointsTo(string? registeredValue, string? exePath)
        {
            if (string.IsNullOrWhiteSpace(registeredValue) || string.IsNullOrEmpty(exePath)) return false;

            string registeredPath = registeredValue.Trim().Trim('"');
            return string.Equals(registeredPath, exePath, StringComparison.OrdinalIgnoreCase);
        }

        /// <param name="registeredValue">Listede şu an yazan değer; kayıt yoksa null.</param>
        /// <param name="exePath">Çalışan exe'nin tam yolu; bilinmiyorsa null.</param>
        /// <param name="shouldRunOnStartup">Kullanıcı "Bilgisayar Açıldığında Otomatik Başlat"ı açtı mı?</param>
        public static StartupEntryAction Decide(string? registeredValue, string? exePath, bool shouldRunOnStartup)
        {
            if (!shouldRunOnStartup)
            {
                return registeredValue != null ? StartupEntryAction.Delete : StartupEntryAction.None;
            }

            // Kendi yolumuzu bilmiyorsak yanlış bir şey yazmaktansa dokunmayalım
            if (string.IsNullOrEmpty(exePath)) return StartupEntryAction.None;

            return PointsTo(registeredValue, exePath) ? StartupEntryAction.None : StartupEntryAction.Write;
        }
    }
}
