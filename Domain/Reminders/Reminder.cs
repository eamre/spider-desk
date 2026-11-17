using System;

namespace SpiderDesk.Domain.Reminders
{
    /// <summary>
    /// Tek bir hatırlatma: ne hatırlatılacağı ve ne zaman.
    /// Id, aynı metin ve saatte iki hatırlatma olsa bile tepsi menüsünden doğru olanı silebilmek içindir.
    /// </summary>
    public sealed record Reminder(Guid Id, string Text, DateTime DueAt)
    {
        /// <summary>Not kartına rahatça sığan en uzun metin.</summary>
        public const int MaxTextLength = 60;

        /// <summary>
        /// Zamanından bu kadar sonra gösterilen hatırlatma "kaçırılmış" sayılır (örneğin bilgisayar o saatte kapalıydı).
        /// Birkaç saniyelik gecikme (örümcek tavana tırmanırken) kaçırmak sayılmaz.
        /// </summary>
        public static readonly TimeSpan MissedAfter = TimeSpan.FromMinutes(1);

        /// <summary>Metin boş değilse ve not kartına sığıyorsa true döner. Pencere "Kaydet" düğmesini buna göre açar.</summary>
        public static bool IsValidText(string? text)
        {
            string cleaned = CleanText(text);
            return cleaned.Length > 0 && cleaned.Length <= MaxTextLength;
        }

        /// <summary>Yeni bir hatırlatma oluşturur. Metnin başındaki/sonundaki boşluklar atılır, satır sonları boşluğa çevrilir.</summary>
        public static Reminder Create(string text, DateTime dueAt)
        {
            string cleaned = CleanText(text);
            if (cleaned.Length == 0) throw new ArgumentException("Hatırlatma metni boş olamaz.", nameof(text));
            if (cleaned.Length > MaxTextLength) throw new ArgumentException($"Hatırlatma metni en fazla {MaxTextLength} karakter olabilir.", nameof(text));

            return new Reminder(Guid.NewGuid(), cleaned, dueAt);
        }

        /// <summary>Hatırlatma, zamanından <see cref="MissedAfter"/> kadar sonra gösteriliyorsa true döner.</summary>
        public bool IsMissedAt(DateTime now) => now - DueAt > MissedAfter;

        private static string CleanText(string? text) =>
            (text ?? string.Empty).Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
    }
}
