using System;

namespace SpiderDesk.Domain.Pomodoro
{
    /// <summary>Pomodoro'nun büyük düğmesinin o an ne yapacağı.</summary>
    public enum PomodoroPrimaryAction
    {
        Start,
        Pause,
        Resume
    }

    /// <summary>Geri sayım halkasının görünümü: hazırken boş, sayarken renkli, duraklatılınca gri.</summary>
    public enum PomodoroLook
    {
        Idle,
        Active,
        Paused
    }

    /// <summary>
    /// Pomodoro sekmesinde o an gösterilecek her şey. <see cref="For"/> zamanlayıcıdan hesaplar; arayüz yalnızca bunu çizer.
    /// </summary>
    /// <param name="Time">Halkanın ortasındaki büyük süre, örneğin "24:13".</param>
    /// <param name="Status">Sürenin altındaki küçük yazı, örneğin "Odak · 1/4".</param>
    /// <param name="Progress">Halkanın ne kadar dolu olacağı: 1 tam, 0 boş.</param>
    /// <param name="PrimaryAction">Büyük düğmenin yapacağı iş (Başlat, Duraklat ya da Devam et).</param>
    /// <param name="CanFinish">Pomodoro çalışıyorsa "Bitir" düğmesi görünür.</param>
    /// <param name="Look">Halkanın rengi.</param>
    public readonly record struct PomodoroDisplay(
        string Time,
        string Status,
        double Progress,
        PomodoroPrimaryAction PrimaryAction,
        bool CanFinish,
        PomodoroLook Look)
    {
        public static PomodoroDisplay For(PomodoroTimer timer, DateTime now)
        {
            if (!timer.IsRunning)
            {
                return new(PomodoroText.Clock(timer.Durations.Focus), "Hazır", 0, PomodoroPrimaryAction.Start, false, PomodoroLook.Idle);
            }

            TimeSpan remaining = timer.Remaining(now);
            string time = PomodoroText.Clock(remaining);

            if (timer.IsPaused)
            {
                return new(time, "Duraklatıldı", ProgressOf(remaining, timer.CurrentPhaseLength),
                    PomodoroPrimaryAction.Resume, true, PomodoroLook.Paused);
            }

            // Tur, örümcek turun yazısını bitirince saymaya başlar; o sırada halka doludur
            if (timer.IsWaitingToStart)
            {
                string word = timer.Phase == PomodoroPhase.Focus ? PomodoroText.FocusWord : PomodoroText.BreakWord;
                return new(time, $"Örümcek {word} yazıyor…", 1, PomodoroPrimaryAction.Pause, true, PomodoroLook.Active);
            }

            return new(time, StatusOf(timer), ProgressOf(remaining, timer.CurrentPhaseLength),
                PomodoroPrimaryAction.Pause, true, PomodoroLook.Active);
        }

        private static string StatusOf(PomodoroTimer timer) => timer.Phase switch
        {
            // Kaçıncı odak turunda olunduğu: dördüncü odaktan sonra uzun mola gelir
            PomodoroPhase.Focus =>
                $"Odak · {timer.CompletedFocusSessions % PomodoroPlan.FocusSessionsBeforeLongBreak + 1}/{PomodoroPlan.FocusSessionsBeforeLongBreak}",
            PomodoroPhase.ShortBreak => "Kısa mola",
            _ => "Uzun mola"
        };

        private static double ProgressOf(TimeSpan remaining, TimeSpan length) =>
            length > TimeSpan.Zero ? Math.Clamp(remaining / length, 0, 1) : 0;
    }
}
