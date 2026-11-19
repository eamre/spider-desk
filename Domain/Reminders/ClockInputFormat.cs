using System;
using System.Linq;

namespace SpiderDesk.Domain.Reminders
{
    /// <summary>
    /// Hatırlatıcının saat kutusuna yazılanı "19:59" biçimine sokar; kullanıcı yalnızca rakam yazar, ayraç kendiliğinden gelir.
    /// "1959" → "19:59", "195" → "19:5". Tek haneli saat için ayraç tuşuna basılabilir: "9." → "9:".
    /// Harfler atılır, en fazla dört rakam alınır. Sonucu <see cref="ReminderTime.TryParseClock"/> okur.
    /// </summary>
    public static class ClockInputFormat
    {
        public const char Separator = ':';

        public static string Format(string? typed)
        {
            string text = typed ?? string.Empty;
            int separator = text.IndexOfAny(new[] { ':', '.' });

            if (separator >= 0)
            {
                string hour = DigitsOf(text[..separator], 2);
                string minute = DigitsOf(text[(separator + 1)..], 2);

                // Ayraçtan önce rakam yoksa ayraç sayılmaz
                if (hour.Length == 0) return FromDigits(DigitsOf(text, 4));

                // İki haneli saatten sonra tek başına kalan ayraç, rakam silinince onunla birlikte gider:
                // "19:5"ten bir rakam silinince "19" kalır (ayraç bir sonraki rakamla yeniden gelir)
                if (minute.Length == 0 && hour.Length == 2) return hour;

                return hour + Separator + minute;
            }

            return FromDigits(DigitsOf(text, 4));
        }

        // "19" → "19", "195" → "19:5", "1959" → "19:59"
        private static string FromDigits(string digits) =>
            digits.Length <= 2 ? digits : digits[..2] + Separator + digits[2..];

        private static string DigitsOf(string text, int max) =>
            new string(text.Where(char.IsAsciiDigit).Take(max).ToArray());
    }
}
