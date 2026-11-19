using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Domain.Pomodoro;
using SpiderDesk.Infrastructure.Sessions;
using SpiderDesk.Infrastructure.Tray;

namespace SpiderDesk.Tests.Infrastructure.Sessions
{
    /// <summary>
    /// Pomodoro sekmesindeki düğmelerin zamanlayıcıyı, örümceği, tepsi ipucunu ve bildirimleri doğru yönettiğini doğrular.
    /// Sekmenin yerine sahte bir görünüm kullanılır; zaman testte ileri sarılır.
    /// </summary>
    public class PomodoroSessionControllerTests
    {
        private sealed class FakeView : IPomodoroView
        {
            public event Action? StartRequested;
            public event Action? PauseRequested;
            public event Action? ResumeRequested;
            public event Action? FinishRequested;
            public event Action<PomodoroPreset>? PresetSelected;

            public PomodoroPreset? ShownPreset { get; private set; }
            public PomodoroDisplay? Shown { get; private set; }

            public void ShowPreset(PomodoroPreset preset) => ShownPreset = preset;
            public void ShowState(PomodoroDisplay display) => Shown = display;

            public void PressStart() => StartRequested?.Invoke();
            public void PressPause() => PauseRequested?.Invoke();
            public void PressResume() => ResumeRequested?.Invoke();
            public void PressFinish() => FinishRequested?.Invoke();
            public void ChoosePreset(PomodoroPreset preset) => PresetSelected?.Invoke(preset);
        }

        private sealed class FakeTray : ITrayTooltip, INotificationService
        {
            public string? Countdown { get; private set; } = "(gösterilmedi)";
            public List<string> Notifications { get; } = new();
            public void ShowPomodoroCountdown(string? countdown) => Countdown = countdown;
            public void ShowNotification(string title, string message) => Notifications.Add(title);
        }

        private sealed class FakeRepository : ISettingsRepository
        {
            public UserSettings? LastSaved { get; private set; }
            public string? LastError => null;
            public UserSettings Load() => new();
            public bool Save(UserSettings settings)
            {
                LastSaved = settings;
                return true;
            }
        }

        private sealed class FakeEnvironment : IEnvironmentService
        {
            public EnvironmentTimeMode TimeMode { get; set; }
            public bool IsNightTime => false;
            public TimeSpan LocalTimeOfDay => TimeSpan.FromHours(10);
        }

        private readonly FakeView _view = new();
        private readonly FakeTray _tray = new();
        private readonly FakeRepository _repository = new();
        private readonly SimulationEngine _engine;
        private readonly PomodoroSessionController _controller;

        public PomodoroSessionControllerTests()
        {
            var settings = new SettingsStore(_repository);
            _engine = new SimulationEngine(new BugSpeciesCatalog(), new FakeEnvironment(), settings.Current, 1920, 1080);
            _controller = new PomodoroSessionController(_engine, settings, _view, _tray, _tray);
        }

        [Fact]
        public void OnOpening_ShowsTheSavedPreset()
        {
            Assert.Equal(PomodoroPreset.Classic, _view.ShownPreset);
        }

        [Fact]
        public void BeforeStarting_TheTabIsReady_AndTheTooltipIsPlain()
        {
            _controller.Tick(DateTime.Now);

            Assert.Equal("Hazır", _view.Shown?.Status);
            Assert.Null(_tray.Countdown);
        }

        [Fact]
        public void Starting_AsksTheSpiderToWriteFocus_AndShowsItAtOnce()
        {
            _view.PressStart();

            Assert.True(_engine.IsBusyWithAnnouncement);
            Assert.Equal("Örümcek FOCUS yazıyor…", _view.Shown?.Status);
            Assert.Equal(PomodoroPrimaryAction.Pause, _view.Shown?.PrimaryAction);
            Assert.Equal("Odak 25:00 ⏳", _tray.Countdown);
            // Kullanıcı başlattığını zaten biliyor: bildirim çıkmaz
            Assert.Empty(_tray.Notifications);
        }

        [Fact]
        public void PausingAndResuming_ChangesTheBigButton()
        {
            _view.PressStart();

            _view.PressPause();
            Assert.Equal(PomodoroPrimaryAction.Resume, _view.Shown?.PrimaryAction);
            Assert.EndsWith("⏸", _tray.Countdown);

            _view.PressResume();
            Assert.Equal(PomodoroPrimaryAction.Pause, _view.Shown?.PrimaryAction);
        }

        [Fact]
        public void Finishing_ResetsEverything_AndTheSpiderDoesNotWriteFocusAnymore()
        {
            _view.PressStart();

            _view.PressFinish();

            Assert.Equal("Hazır", _view.Shown?.Status);
            Assert.False(_view.Shown?.CanFinish);
            Assert.Null(_tray.Countdown);
            Assert.False(_engine.IsBusyWithAnnouncement);
        }

        [Fact]
        public void WhenTheFocusEnds_ABreakIsAnnounced_WithANotification()
        {
            DateTime t0 = DateTime.Now;
            _view.PressStart();

            // Örümcek bu testte hiç hareket etmediği için FOCUS'u yazamaz; Pomodoro en fazla 60 sn bekleyip kendisi başlar.
            // Başlat'a bu satırlardan biraz sonra basıldığı için bir saniye pay bırakılır.
            DateTime countdownStarts = t0 + PomodoroPlan.MaxWaitForAnnouncement + TimeSpan.FromSeconds(1);
            _controller.Tick(countdownStarts);
            _controller.Tick(countdownStarts + TimeSpan.FromMinutes(25));

            Assert.Single(_tray.Notifications);
            Assert.Equal("Örümcek BREAK yazıyor…", _view.Shown?.Status);
        }

        [Fact]
        public void ChoosingTheLongPreset_SavesIt_AndShowsFiftyMinutes()
        {
            _view.ChoosePreset(PomodoroPreset.Extended);

            Assert.Equal(PomodoroPreset.Extended, _repository.LastSaved?.PomodoroPreset);
            Assert.Equal("50:00", _view.Shown?.Time);
        }
    }
}
