using System;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Pomodoro;
using SpiderDesk.Infrastructure.Tray;

namespace SpiderDesk.Infrastructure.Sessions
{
    /// <summary>
    /// Pomodoro zamanlayıcısını Pomodoro sekmesine (<see cref="IPomodoroView"/>), örümceğe, tepsi ikonunun ipucuna
    /// ve Windows bildirimlerine bağlar. Her karede <see cref="Tick"/> ile o anki saat verilir;
    /// tur değişince örümcek FOCUS/BREAK yazar ve bildirim gelir.
    /// </summary>
    public sealed class PomodoroSessionController
    {
        private readonly PomodoroTimer _timer;
        private readonly SimulationEngine _engine;
        private readonly SettingsStore _settings;
        private readonly IPomodoroView _view;
        private readonly ITrayTooltip _tooltip;
        private readonly INotificationService _notifications;

        public PomodoroSessionController(
            SimulationEngine engine,
            SettingsStore settings,
            IPomodoroView view,
            ITrayTooltip tooltip,
            INotificationService notifications)
        {
            _engine = engine;
            _settings = settings;
            _view = view;
            _tooltip = tooltip;
            _notifications = notifications;
            _timer = new PomodoroTimer(PomodoroPlan.For(settings.Current.PomodoroPreset));

            _view.ShowPreset(settings.Current.PomodoroPreset);
            _view.StartRequested += Start;
            _view.PauseRequested += () => Change(now => _timer.Pause(now));
            _view.ResumeRequested += () => Change(now => _timer.Resume(now));
            _view.FinishRequested += Finish;
            _view.PresetSelected += SelectPreset;
        }

        /// <summary>Kare döngüsünden çağrılır. Uygulama duraklatılmış olsa bile çağrılmalıdır ki süre işlesin.</summary>
        public void Tick(DateTime now)
        {
            if (_timer.Update(now) is { } newPhase)
            {
                Announce(newPhase, _timer.Remaining(now));
            }

            // Yeni turun süresi, örümcek turun yazısını (FOCUS/BREAK) yazıp el sallamayı bitirince başlar
            if (_timer.IsWaitingToStart && !_engine.IsBusyWithAnnouncement)
            {
                _timer.BeginCountdown(now);
            }

            ShowState(now);
        }

        private void Start() => Change(now =>
        {
            // Kullanıcı başlattığını zaten biliyor: bildirim yerine yalnızca örümcek FOCUS yazar
            _timer.Start(now);
            _engine.RequestSilkMessage(PomodoroText.FocusWord);
        });

        private void Finish() => Change(_ =>
        {
            _timer.Stop();
            // Örümcek FOCUS/BREAK yazmaya henüz başlamadıysa artık yazmasın
            _engine.CancelPendingSilkMessage();
        });

        /// <summary>Zamanlayıcıyı o anki saatle değiştirir ve sekmeyi hemen günceller (bir sonraki kareyi beklemeden).</summary>
        private void Change(Action<DateTime> change)
        {
            DateTime now = DateTime.Now;
            change(now);
            ShowState(now);
        }

        private void SelectPreset(PomodoroPreset preset)
        {
            _timer.SetDurations(PomodoroPlan.For(preset));
            _settings.Update(s => s.PomodoroPreset = preset);
            ShowState(DateTime.Now);
        }

        private void Announce(PomodoroPhase phase, TimeSpan length)
        {
            PomodoroAnnouncement announcement = PomodoroText.AnnouncementFor(phase, length);
            _engine.RequestSilkMessage(announcement.SilkWord);
            _notifications.ShowNotification(announcement.Title, announcement.Message);
        }

        private void ShowState(DateTime now)
        {
            _view.ShowState(PomodoroDisplay.For(_timer, now));
            _tooltip.ShowPomodoroCountdown(_timer.IsRunning
                ? PomodoroText.Countdown(_timer.Phase, _timer.Remaining(now), _timer.IsWaitingToStart, _timer.IsPaused)
                : null);
        }
    }
}
