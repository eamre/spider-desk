using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Pomodoro;

namespace SpiderDesk.Tests.Domain.Pomodoro
{
    /// <summary>
    /// Pomodoro sekmesinde ne görüneceğini doğrular: halkadaki süre, altındaki durum yazısı, halkanın doluluğu,
    /// büyük düğmenin ne yazacağı ve "Bitir"in görünüp görünmeyeceği.
    /// </summary>
    public class PomodoroDisplayTests
    {
        private static readonly DateTime T0 = new(2026, 1, 1, 9, 0, 0);
        private static DateTime At(double minutes) => T0.AddMinutes(minutes);

        private static PomodoroTimer Timer(PomodoroPreset preset = PomodoroPreset.Classic) => new(PomodoroPlan.For(preset));

        private static PomodoroTimer Counting()
        {
            PomodoroTimer timer = Timer();
            timer.Start(T0);
            timer.BeginCountdown(T0);
            return timer;
        }

        [Fact]
        public void BeforeStarting_ShowsTheFullFocusLength_AndAStartButton()
        {
            PomodoroDisplay display = PomodoroDisplay.For(Timer(), T0);

            Assert.Equal("25:00", display.Time);
            Assert.Equal("Hazır", display.Status);
            Assert.Equal(0, display.Progress);
            Assert.Equal(PomodoroPrimaryAction.Start, display.PrimaryAction);
            Assert.False(display.CanFinish);
            Assert.Equal(PomodoroLook.Idle, display.Look);
        }

        [Fact]
        public void BeforeStarting_TheLongPresetShowsFiftyMinutes()
        {
            Assert.Equal("50:00", PomodoroDisplay.For(Timer(PomodoroPreset.Extended), T0).Time);
        }

        [Fact]
        public void WhileTheSpiderWritesFocus_TheRingIsFull_AndSaysSo()
        {
            PomodoroTimer timer = Timer();
            timer.Start(T0);

            PomodoroDisplay display = PomodoroDisplay.For(timer, At(0.2));

            Assert.Equal("25:00", display.Time);
            Assert.Equal("Örümcek FOCUS yazıyor…", display.Status);
            Assert.Equal(1, display.Progress);
            Assert.Equal(PomodoroPrimaryAction.Pause, display.PrimaryAction);
            Assert.True(display.CanFinish);
        }

        [Fact]
        public void HalfwayThroughTheFirstFocus_TheRingIsHalfFull()
        {
            PomodoroDisplay display = PomodoroDisplay.For(Counting(), At(12.5));

            Assert.Equal("12:30", display.Time);
            Assert.Equal("Odak · 1/4", display.Status);
            Assert.Equal(0.5, display.Progress, precision: 6);
            Assert.Equal(PomodoroLook.Active, display.Look);
        }

        [Fact]
        public void AfterTheFirstFocus_ComesAShortBreak_ThenTheSecondFocus()
        {
            PomodoroTimer timer = Counting();
            timer.Update(At(25));
            Assert.Equal("Örümcek BREAK yazıyor…", PomodoroDisplay.For(timer, At(25)).Status);

            timer.BeginCountdown(At(25));
            Assert.Equal("Kısa mola", PomodoroDisplay.For(timer, At(26)).Status);

            timer.Update(At(30));
            timer.BeginCountdown(At(30));
            Assert.Equal("Odak · 2/4", PomodoroDisplay.For(timer, At(31)).Status);
        }

        [Fact]
        public void AfterFourFocusSessions_ComesALongBreak()
        {
            PomodoroTimer timer = Counting();
            double t = 0;
            for (int focus = 0; focus < 4; focus++)
            {
                t += 25;
                timer.Update(At(t));
                timer.BeginCountdown(At(t));
                if (focus < 3)
                {
                    t += 5;
                    timer.Update(At(t));
                    timer.BeginCountdown(At(t));
                }
            }

            Assert.Equal("Uzun mola", PomodoroDisplay.For(timer, At(t + 1)).Status);
        }

        [Fact]
        public void WhenPaused_TheRingTurnsGrey_AndTheButtonSaysResume()
        {
            PomodoroTimer timer = Counting();
            timer.Pause(At(10));

            PomodoroDisplay display = PomodoroDisplay.For(timer, At(40));

            Assert.Equal("15:00", display.Time);
            Assert.Equal("Duraklatıldı", display.Status);
            Assert.Equal(0.6, display.Progress, precision: 6);
            Assert.Equal(PomodoroPrimaryAction.Resume, display.PrimaryAction);
            Assert.Equal(PomodoroLook.Paused, display.Look);
            Assert.True(display.CanFinish);
        }

        [Fact]
        public void ChangingThePresetMidSession_DoesNotOverfillTheRing()
        {
            PomodoroTimer timer = Counting();
            timer.SetDurations(PomodoroPlan.For(PomodoroPreset.Extended));

            PomodoroDisplay display = PomodoroDisplay.For(timer, At(5));

            // Süren tur hâlâ 25 dakikalık; yeni süre bir sonraki turda geçerli
            Assert.Equal("20:00", display.Time);
            Assert.Equal(0.8, display.Progress, precision: 6);
        }

        [Fact]
        public void AfterFinishing_ItIsReadyAgain()
        {
            PomodoroTimer timer = Counting();
            timer.Stop();

            PomodoroDisplay display = PomodoroDisplay.For(timer, At(3));

            Assert.Equal("Hazır", display.Status);
            Assert.False(display.CanFinish);
            Assert.Equal(PomodoroPrimaryAction.Start, display.PrimaryAction);
        }
    }
}
