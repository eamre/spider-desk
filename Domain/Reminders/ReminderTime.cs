using System;

namespace SpiderDesk.Domain.Reminders
{
    /// <summary>
    /// Pencerede seçilen zamanı ("15 dk sonra" ya da "14:30") gerçek bir tarihe çevirir.
    /// </summary>
    public static class ReminderTime
    {
        /// <summary>Pencerede tek tıkla seçilebilen süreler.</summary>
        public static readonly TimeSpan[] QuickDelays =
        {
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1)
        };

        /// <summary>Şu andan belli bir süre sonrası (örneğin "15 dk sonra").</summary>
        public static DateTime After(DateTime now, TimeSpan delay) => now + delay;

        /// <summary>
        /// Bugün verilen saatte; o saat bugün geçtiyse (ya da tam şu dakikaysa) yarın aynı saatte.
        /// Örneğin saat 16:00'da "09:00" yazılırsa hatırlatma yarın sabah 09:00'a kurulur.
        /// </summary>
        public static DateTime At(DateTime now, TimeOnly clock)
        {
            DateTime today = now.Date + clock.ToTimeSpan();
            return today > now ? today : today.AddDays(1);
        }

        /// <summary>
        /// Kullanıcının yazdığı saati okur. "14:30", "14.30", "1430", "9:05" ve sadece saat ("9") kabul edilir.
        /// Geçersizse (örneğin "25:00" ya da "abc") false döner.
        /// </summary>
        public static bool TryParseClock(string? input, out TimeOnly clock)
        {
            clock = default;
            string text = (input ?? string.Empty).Trim();
            if (text.Length == 0) return false;

            int hour;
            int minute;
            int separator = text.IndexOfAny(new[] { ':', '.' });

            if (separator >= 0)
            {
                // "14:30" ya da "14.30": ayraçtan önce saat, sonra iki haneli dakika
                string minutePart = text[(separator + 1)..];
                if (minutePart.Length != 2
                    || !int.TryParse(text[..separator], out hour)
                    || !int.TryParse(minutePart, out minute))
                {
                    return false;
                }
            }
            else
            {
                if (!int.TryParse(text, out int number) || text.Length > 4) return false;

                // "9" ya da "14" yalnızca saattir; "930" ya da "1430" ise son iki hane dakikadır
                if (text.Length <= 2)
                {
                    hour = number;
                    minute = 0;
                }
                else
                {
                    hour = number / 100;
                    minute = number % 100;
                }
            }

            if (hour is < 0 or > 23 || minute is < 0 or > 59) return false;

            clock = new TimeOnly(hour, minute);
            return true;
        }
    }
}
