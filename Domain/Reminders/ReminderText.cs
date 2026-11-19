using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpiderDesk.Domain.Reminders
{
    public readonly record struct ReminderNotification(string Title, string Message);

    /// <summary>
    /// Hatırlatmalarla ilgili kullanıcıya gösterilen bütün yazılar: listedeki zaman, bildirim ve örümceğin notu.
    /// Saat dışarıdan verilir (now); böylece "Bugün" / "Yarın" gibi ifadeler testlerde denenebilir.
    /// </summary>
    public static class ReminderText
    {
        public const string MissedNotePrefix = "Kaçırdın: ";

        private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

        /// <summary>Hızlı süre düğmesinin yazısı: "5 dk", "15 dk", "1 saat", "1,5 saat".</summary>
        public static string DelayLabel(TimeSpan delay) =>
            delay.TotalMinutes < 60
                ? $"{delay.TotalMinutes.ToString("0", Turkish)} dk"
                : $"{delay.TotalHours.ToString("0.#", Turkish)} saat";

        /// <summary>Bir zamanın kısa, okunur hali: "Bugün 14:30", "Yarın 09:00" ya da "20 Kasım 09:00".</summary>
        public static string When(DateTime dueAt, DateTime now)
        {
            string clock = dueAt.ToString("HH:mm", Turkish);
            int daysAway = (dueAt.Date - now.Date).Days;

            return daysAway switch
            {
                0 => $"Bugün {clock}",
                1 => $"Yarın {clock}",
                -1 => $"Dün {clock}",
                _ => $"{dueAt.ToString("d MMMM", Turkish)} {clock}"
            };
        }

        /// <summary>
        /// Zamanı gelen hatırlatmalar için tek bir Windows bildirimi. Bildirimler birbirinin üstüne yazıldığı için
        /// aynı anda birden fazla hatırlatma gelirse hepsi tek bildirimde alt alta listelenir.
        /// </summary>
        public static ReminderNotification Notification(IReadOnlyList<Reminder> due, DateTime now)
        {
            if (due.Count == 0) throw new ArgumentException("En az bir hatırlatma olmalı.", nameof(due));

            if (due.Count == 1)
            {
                Reminder only = due[0];
                return only.IsMissedAt(now)
                    ? new ReminderNotification("⏰ Kaçırılan hatırlatma", $"{only.Text} ({When(only.DueAt, now)})")
                    : new ReminderNotification("⏰ Hatırlatma", only.Text);
            }

            string lines = string.Join(Environment.NewLine, due.Select(reminder =>
                reminder.IsMissedAt(now) ? $"• {reminder.Text} ({When(reminder.DueAt, now)})" : $"• {reminder.Text}"));
            return new ReminderNotification($"⏰ {due.Count} hatırlatma", lines);
        }

        /// <summary>Örümceğin tuttuğu notta yazan metin. Kaçırılmış hatırlatmanın başına "Kaçırdın: " eklenir.</summary>
        public static string NoteText(Reminder reminder, DateTime now) =>
            reminder.IsMissedAt(now) ? MissedNotePrefix + reminder.Text : reminder.Text;
    }
}
