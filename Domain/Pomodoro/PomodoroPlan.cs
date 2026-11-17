using System;
using SpiderDesk.Core.Settings;

namespace SpiderDesk.Domain.Pomodoro
{
    public readonly record struct PomodoroDurations(TimeSpan Focus, TimeSpan ShortBreak, TimeSpan LongBreak);

    /// <summary>
    /// Kullanıcının seçtiği Pomodoro ayarını odak ve mola sürelerine çevirir.
    /// </summary>
    public static class PomodoroPlan
    {
        /// <summary>Bu kadar odak turu tamamlanınca kısa mola yerine uzun mola verilir.</summary>
        public const int FocusSessionsBeforeLongBreak = 4;

        public static PomodoroDurations For(PomodoroPreset preset) => preset switch
        {
            PomodoroPreset.Extended => new(TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20)),
            _ => new(TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15))
        };
    }
}
