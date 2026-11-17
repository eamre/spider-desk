using System;

namespace SpiderDesk.Domain.Pomodoro
{
    /// <summary>
    /// Odak ve mola turlarını sırayla yürüten zamanlayıcı.
    /// Kare döngüsüne değil gerçek saate bakar: o anki saat her çağrıda dışarıdan verilir.
    /// Böylece uygulama duraklatılsa ya da ekran kapalı olsa bile süre doğru işler ve testte zaman ileri sarılabilir.
    /// </summary>
    public sealed class PomodoroTimer
    {
        private PomodoroDurations _durations;
        private DateTime _phaseEndsAt;

        public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Idle;

        /// <summary>Son başlatmadan beri tamamlanan odak turu sayısı.</summary>
        public int CompletedFocusSessions { get; private set; }

        public bool IsRunning => Phase != PomodoroPhase.Idle;

        public PomodoroTimer(PomodoroDurations durations)
        {
            _durations = durations;
        }

        /// <summary>Yeni süreler o an süren turu değiştirmez; bir sonraki turdan itibaren geçerli olur.</summary>
        public void SetDurations(PomodoroDurations durations) => _durations = durations;

        /// <summary>İlk odak turunu başlatır. Zamanlayıcı zaten çalışıyorsa baştan başlar.</summary>
        public void Start(DateTime now)
        {
            CompletedFocusSessions = 0;
            EnterPhase(PomodoroPhase.Focus, now);
        }

        public void Stop()
        {
            Phase = PomodoroPhase.Idle;
            CompletedFocusSessions = 0;
        }

        /// <summary>
        /// Süren turun süresi dolduysa sıradaki tura geçer ve yeni turu döner; dolmadıysa null döner.
        /// Bilgisayar uykudan uyanınca birden çok tur atlanmaz: tek bir geçiş yapılır ve yeni tur şimdiden başlar.
        /// </summary>
        public PomodoroPhase? Update(DateTime now)
        {
            if (!IsRunning || now < _phaseEndsAt) return null;

            PomodoroPhase next;
            if (Phase == PomodoroPhase.Focus)
            {
                CompletedFocusSessions++;
                next = CompletedFocusSessions % PomodoroPlan.FocusSessionsBeforeLongBreak == 0
                    ? PomodoroPhase.LongBreak
                    : PomodoroPhase.ShortBreak;
            }
            else
            {
                next = PomodoroPhase.Focus;
            }

            EnterPhase(next, now);
            return next;
        }

        public TimeSpan Remaining(DateTime now)
        {
            if (!IsRunning) return TimeSpan.Zero;
            TimeSpan left = _phaseEndsAt - now;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        private void EnterPhase(PomodoroPhase phase, DateTime now)
        {
            Phase = phase;
            _phaseEndsAt = now + DurationOf(phase);
        }

        private TimeSpan DurationOf(PomodoroPhase phase) => phase switch
        {
            PomodoroPhase.Focus => _durations.Focus,
            PomodoroPhase.ShortBreak => _durations.ShortBreak,
            PomodoroPhase.LongBreak => _durations.LongBreak,
            _ => TimeSpan.Zero
        };
    }
}
