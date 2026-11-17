using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Pomodoro;

namespace SpiderDesk.Tests.Domain.Pomodoro
{
    public class PomodoroTimerTests
    {
        // Testlerde gerçek saat yerine sabit bir başlangıç anı kullanıyoruz; "zaman geçti" demek için dakika ekliyoruz
        private static readonly DateTime T0 = new(2026, 1, 1, 9, 0, 0);
        private static readonly PomodoroDurations Classic = PomodoroPlan.For(PomodoroPreset.Classic);

        private static DateTime At(double minutes) => T0.AddMinutes(minutes);

        // Örümceğin FOCUS yazısını hemen bitirdiği durum: süre başlatıldığı anda saymaya başlar
        private static PomodoroTimer StartedTimer()
        {
            var timer = new PomodoroTimer(Classic);
            timer.Start(T0);
            timer.BeginCountdown(T0);
            return timer;
        }

        // Tur değiştiyse örümceğin yeni turun yazısını hemen bitirdiğini varsayar
        private static PomodoroPhase? Advance(PomodoroTimer timer, DateTime now)
        {
            PomodoroPhase? next = timer.Update(now);
            timer.BeginCountdown(now);
            return next;
        }

        // ---------- Başlatma ve durdurma ----------

        [Fact]
        public void NewTimer_IsIdle_WithNoTimeLeft()
        {
            var timer = new PomodoroTimer(Classic);

            Assert.Equal(PomodoroPhase.Idle, timer.Phase);
            Assert.False(timer.IsRunning);
            Assert.Equal(TimeSpan.Zero, timer.Remaining(T0));
            Assert.Null(timer.Update(At(100)));
        }

        [Fact]
        public void Start_BeginsAFullFocusSession()
        {
            var timer = StartedTimer();

            Assert.Equal(PomodoroPhase.Focus, timer.Phase);
            Assert.Equal(TimeSpan.FromMinutes(25), timer.Remaining(T0));
            Assert.Equal(TimeSpan.FromMinutes(10), timer.Remaining(At(15)));
        }

        [Fact]
        public void Stop_MakesTheTimerIdle()
        {
            var timer = StartedTimer();

            timer.Stop();

            Assert.False(timer.IsRunning);
            Assert.False(timer.IsWaitingToStart);
            Assert.Equal(TimeSpan.Zero, timer.Remaining(At(5)));
            Assert.Null(timer.Update(At(60)));
        }

        [Fact]
        public void StartAgain_RestartsFromAFreshFocusSession()
        {
            var timer = StartedTimer();
            Advance(timer, At(25));

            timer.Start(At(27));
            timer.BeginCountdown(At(27));

            Assert.Equal(PomodoroPhase.Focus, timer.Phase);
            Assert.Equal(0, timer.CompletedFocusSessions);
            Assert.Equal(TimeSpan.FromMinutes(25), timer.Remaining(At(27)));
        }

        // ---------- Örümceğin yazısını beklemek ----------

        [Fact]
        public void Start_WaitsForTheSpider_BeforeCounting()
        {
            var timer = new PomodoroTimer(Classic);

            timer.Start(T0);

            Assert.True(timer.IsWaitingToStart);
            Assert.Equal(TimeSpan.FromMinutes(25), timer.Remaining(At(0.5)));
            Assert.Null(timer.Update(At(0.5)));
            Assert.True(timer.IsWaitingToStart);
        }

        [Fact]
        public void BeginCountdown_StartsCountingFromThatMoment()
        {
            var timer = new PomodoroTimer(Classic);
            timer.Start(T0);

            // Örümcek FOCUS yazısını 30 saniyede bitirdi
            timer.BeginCountdown(At(0.5));

            Assert.False(timer.IsWaitingToStart);
            Assert.Equal(TimeSpan.FromMinutes(25), timer.Remaining(At(0.5)));
            Assert.Null(timer.Update(At(25.4)));
            Assert.Equal(PomodoroPhase.ShortBreak, timer.Update(At(25.5)));
        }

        [Fact]
        public void BeginCountdown_WhileAlreadyCounting_DoesNotRestartTheClock()
        {
            var timer = StartedTimer();

            timer.BeginCountdown(At(10));

            Assert.Equal(TimeSpan.FromMinutes(15), timer.Remaining(At(10)));
        }

        [Fact]
        public void BeginCountdown_WhenIdle_DoesNothing()
        {
            var timer = new PomodoroTimer(Classic);

            timer.BeginCountdown(T0);

            Assert.False(timer.IsRunning);
            Assert.False(timer.IsWaitingToStart);
        }

        [Fact]
        public void NewPhase_AlsoWaitsForTheSpider()
        {
            var timer = StartedTimer();

            Assert.Equal(PomodoroPhase.ShortBreak, timer.Update(At(25)));

            Assert.True(timer.IsWaitingToStart);
            Assert.Equal(TimeSpan.FromMinutes(5), timer.Remaining(At(25.5)));
        }

        [Fact]
        public void WaitingTooLong_StartsCountingAnyway()
        {
            var timer = new PomodoroTimer(Classic);
            timer.Start(T0);
            double limit = PomodoroPlan.MaxWaitForAnnouncement.TotalMinutes;

            // Örümcek duraklatılmış ve hiç yazamıyor
            timer.Update(At(limit - 0.01));
            Assert.True(timer.IsWaitingToStart);

            timer.Update(At(limit));
            Assert.False(timer.IsWaitingToStart);
            Assert.Equal(TimeSpan.FromMinutes(25), timer.Remaining(At(limit)));
        }

        // ---------- Tur geçişleri ----------

        [Fact]
        public void Update_BeforeTheEnd_ChangesNothing()
        {
            var timer = StartedTimer();

            Assert.Null(timer.Update(At(24.99)));
            Assert.Equal(PomodoroPhase.Focus, timer.Phase);
        }

        [Fact]
        public void FocusEnds_ShortBreakStarts_ThenFocusAgain()
        {
            var timer = StartedTimer();

            Assert.Equal(PomodoroPhase.ShortBreak, Advance(timer, At(25)));
            Assert.Equal(TimeSpan.FromMinutes(5), timer.Remaining(At(25)));

            Assert.Equal(PomodoroPhase.Focus, Advance(timer, At(30)));
            Assert.Equal(1, timer.CompletedFocusSessions);
        }

        [Fact]
        public void FourthFocusSession_IsFollowedByALongBreak()
        {
            var timer = StartedTimer();
            DateTime now = T0;

            for (int session = 1; session <= 3; session++)
            {
                now = now.AddMinutes(25);
                Assert.Equal(PomodoroPhase.ShortBreak, Advance(timer, now));
                now = now.AddMinutes(5);
                Assert.Equal(PomodoroPhase.Focus, Advance(timer, now));
            }

            now = now.AddMinutes(25);
            Assert.Equal(PomodoroPhase.LongBreak, Advance(timer, now));
            Assert.Equal(TimeSpan.FromMinutes(15), timer.Remaining(now));

            now = now.AddMinutes(15);
            Assert.Equal(PomodoroPhase.Focus, Advance(timer, now));
            Assert.Equal(4, timer.CompletedFocusSessions);
        }

        [Fact]
        public void ExtendedPreset_Uses50And10Minutes()
        {
            var timer = new PomodoroTimer(PomodoroPlan.For(PomodoroPreset.Extended));
            timer.Start(T0);
            timer.BeginCountdown(T0);

            Assert.Equal(TimeSpan.FromMinutes(50), timer.Remaining(T0));
            Assert.Equal(PomodoroPhase.ShortBreak, Advance(timer, At(50)));
            Assert.Equal(TimeSpan.FromMinutes(10), timer.Remaining(At(50)));
        }

        [Fact]
        public void ChangingDurations_AppliesFromTheNextPhase()
        {
            var timer = StartedTimer();

            timer.SetDurations(PomodoroPlan.For(PomodoroPreset.Extended));

            Assert.Equal(TimeSpan.FromMinutes(25), timer.Remaining(T0));
            Assert.Equal(PomodoroPhase.ShortBreak, Advance(timer, At(25)));
            Assert.Equal(TimeSpan.FromMinutes(10), timer.Remaining(At(25)));
        }

        [Fact]
        public void AfterSleepingForHours_AdvancesOnlyOnePhase()
        {
            var timer = StartedTimer();

            Assert.Equal(PomodoroPhase.ShortBreak, Advance(timer, At(180)));
            Assert.Equal(TimeSpan.FromMinutes(5), timer.Remaining(At(180)));
            Assert.Equal(1, timer.CompletedFocusSessions);
        }
    }
}
