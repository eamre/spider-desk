using System;

namespace SpiderDesk.Domain.Pomodoro
{
    /// <summary>
    /// Odak ve mola turlarını sırayla yürüten zamanlayıcı.
    /// Kare döngüsüne değil gerçek saate bakar: o anki saat her çağrıda dışarıdan verilir.
    /// Böylece uygulama duraklatılsa ya da ekran kapalı olsa bile süre doğru işler ve testte zaman ileri sarılabilir.
    /// Her yeni tur önce "başlamayı bekler"; örümcek turun yazısını bitirince <see cref="BeginCountdown"/> ile saymaya başlar.
    /// </summary>
    public sealed class PomodoroTimer
    {
        private PomodoroDurations _durations;
        private DateTime _phaseEndsAt;
        private DateTime _waitingSince;

        public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Idle;

        /// <summary>Son başlatmadan beri tamamlanan odak turu sayısı.</summary>
        public int CompletedFocusSessions { get; private set; }

        public bool IsRunning => Phase != PomodoroPhase.Idle;

        /// <summary>Tur başladı ama süre henüz saymıyor: örümceğin turun yazısını bitirmesi bekleniyor.</summary>
        public bool IsWaitingToStart { get; private set; }

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
            IsWaitingToStart = false;
            CompletedFocusSessions = 0;
        }

        /// <summary>Bekleyen turun süresini şimdi başlatır. Tur zaten sayıyorsa hiçbir şey yapmaz.</summary>
        public void BeginCountdown(DateTime now)
        {
            if (!IsRunning || !IsWaitingToStart) return;

            IsWaitingToStart = false;
            _phaseEndsAt = now + DurationOf(Phase);
        }

        /// <summary>
        /// Süren turun süresi dolduysa sıradaki tura geçer ve yeni turu döner; dolmadıysa null döner.
        /// Bekleyen bir tur <see cref="PomodoroPlan.MaxWaitForAnnouncement"/> kadar beklediyse kendiliğinden saymaya başlar.
        /// Bilgisayar uykudan uyanınca birden çok tur atlanmaz: tek bir geçiş yapılır.
        /// </summary>
        public PomodoroPhase? Update(DateTime now)
        {
            if (!IsRunning) return null;

            if (IsWaitingToStart)
            {
                if (now - _waitingSince >= PomodoroPlan.MaxWaitForAnnouncement)
                {
                    BeginCountdown(now);
                }
                return null;
            }

            if (now < _phaseEndsAt) return null;

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

        /// <summary>Turun kalan süresi. Tur henüz saymaya başlamadıysa turun tam süresidir.</summary>
        public TimeSpan Remaining(DateTime now)
        {
            if (!IsRunning) return TimeSpan.Zero;
            if (IsWaitingToStart) return DurationOf(Phase);

            TimeSpan left = _phaseEndsAt - now;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        private void EnterPhase(PomodoroPhase phase, DateTime now)
        {
            Phase = phase;
            IsWaitingToStart = true;
            _waitingSince = now;
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
