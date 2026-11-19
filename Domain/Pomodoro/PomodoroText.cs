using System;

namespace SpiderDesk.Domain.Pomodoro
{
    /// <summary>
    /// Bir tur başladığında yapılacak duyuru: örümceğin ipekle yazacağı kelime ve Windows bildiriminin metni.
    /// </summary>
    public readonly record struct PomodoroAnnouncement(string SilkWord, string Title, string Message);

    /// <summary>
    /// Pomodoro'nun kullanıcıya gösterilen bütün metinlerini üretir: geri sayım ve tur duyuruları.
    /// </summary>
    public static class PomodoroText
    {
        public const string FocusWord = "FOCUS";
        public const string BreakWord = "BREAK";

        /// <summary>
        /// Örneğin "Odak 18:32". Saniyeler yukarı yuvarlanır: tur başında tam süre, son saniyede 00:01 görünür.
        /// Tur, örümceğin yazısını beklerken sonuna ⏳, duraklatılmışken ⏸ eklenir: "Odak 25:00 ⏳", "Odak 18:32 ⏸".
        /// </summary>
        public static string Countdown(PomodoroPhase phase, TimeSpan remaining, bool isWaitingToStart = false, bool isPaused = false)
        {
            string text = $"{LabelOf(phase)} {Clock(remaining)}";
            if (isPaused) return text + " ⏸";
            return isWaitingToStart ? text + " ⏳" : text;
        }

        /// <summary>Kalan süre "18:32" biçiminde. Saniyeler yukarı yuvarlanır: son saniyede "00:01" görünür.</summary>
        public static string Clock(TimeSpan remaining)
        {
            int totalSeconds = (int)Math.Ceiling(Math.Max(0, remaining.TotalSeconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        /// <summary>Yeni başlayan tur için duyuru üretir. <paramref name="length"/> o turun toplam süresidir.</summary>
        public static PomodoroAnnouncement AnnouncementFor(PomodoroPhase phase, TimeSpan length)
        {
            int minutes = (int)Math.Round(length.TotalMinutes);
            return phase switch
            {
                PomodoroPhase.ShortBreak => new(BreakWord, "☕ Mola zamanı", $"Güzel iş! {minutes} dakika mola ver."),
                PomodoroPhase.LongBreak => new(BreakWord, "🌿 Uzun mola",
                    $"{PomodoroPlan.FocusSessionsBeforeLongBreak} tur tamam! {minutes} dakika dinlen."),
                _ => new(FocusWord, "🍅 Odaklanma zamanı", $"{minutes} dakika odaklan. Kolay gelsin!")
            };
        }

        private static string LabelOf(PomodoroPhase phase) => phase switch
        {
            PomodoroPhase.Focus => "Odak",
            PomodoroPhase.ShortBreak => "Mola",
            PomodoroPhase.LongBreak => "Uzun Mola",
            _ => "Pomodoro"
        };
    }
}
