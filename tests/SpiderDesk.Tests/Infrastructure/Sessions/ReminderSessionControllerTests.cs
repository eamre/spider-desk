using SpiderDesk.Core.Services;
using SpiderDesk.Core.Settings;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Domain.Reminders;
using SpiderDesk.Infrastructure.Sessions;
using SpiderDesk.Infrastructure.Tray;

namespace SpiderDesk.Tests.Infrastructure.Sessions
{
    /// <summary>
    /// Hatırlatıcılar sekmesinde eklenen ve silinen hatırlatmaların kaydedildiğini, listenin güncellendiğini
    /// ve zamanı gelen hatırlatmanın bildirimle ve örümceğin notuyla geldiğini doğrular.
    /// </summary>
    public class ReminderSessionControllerTests
    {
        private sealed class FakeView : IReminderView
        {
            public event Action<Reminder>? ReminderCreated;
            public event Action<Guid>? DeleteRequested;

            public IReadOnlyList<ReminderListItem> Shown { get; private set; } = Array.Empty<ReminderListItem>();
            public int ShowCount { get; private set; }

            public void ShowReminders(IReadOnlyList<ReminderListItem> reminders)
            {
                Shown = reminders;
                ShowCount++;
            }

            public void Create(Reminder reminder) => ReminderCreated?.Invoke(reminder);
            public void Delete(Guid id) => DeleteRequested?.Invoke(id);
        }

        private sealed class FakeRepository : IReminderRepository
        {
            public List<Reminder> Stored { get; } = new();
            public int SaveCount { get; private set; }
            public IReadOnlyList<Reminder> Load() => Stored.ToList();
            public bool Save(IEnumerable<Reminder> reminders)
            {
                SaveCount++;
                Stored.Clear();
                Stored.AddRange(reminders);
                return true;
            }
        }

        private sealed class FakeNotifications : INotificationService
        {
            public List<string> Messages { get; } = new();
            public void ShowNotification(string title, string message) => Messages.Add(message);
        }

        private sealed class FakeEnvironment : IEnvironmentService
        {
            public EnvironmentTimeMode TimeMode { get; set; }
            public bool IsNightTime => false;
            public TimeSpan LocalTimeOfDay => TimeSpan.FromHours(10);
        }

        private readonly FakeView _view = new();
        private readonly FakeRepository _repository = new();
        private readonly FakeNotifications _notifications = new();
        private readonly SimulationEngine _engine = new(new BugSpeciesCatalog(), new FakeEnvironment(), new UserSettings(), 1920, 1080);

        private ReminderSessionController Controller() => new(_engine, _repository, _view, _notifications);

        [Fact]
        public void OnOpening_ShowsTheSavedReminders_SoonestFirst()
        {
            DateTime now = DateTime.Now;
            _repository.Stored.Add(Reminder.Create("Faturayı öde", now.AddHours(3)));
            _repository.Stored.Add(Reminder.Create("Su iç", now.AddMinutes(20)));

            Controller();

            Assert.Equal(new[] { "Su iç", "Faturayı öde" }, _view.Shown.Select(r => r.Text));
            Assert.Equal(ReminderText.When(now.AddMinutes(20), now), _view.Shown[0].When);
        }

        [Fact]
        public void AddingAReminder_SavesIt_AndShowsItInTheList()
        {
            Controller();
            var reminder = Reminder.Create("Toplantıya hazırlan", DateTime.Now.AddMinutes(15));

            _view.Create(reminder);

            Assert.Equal(1, _repository.SaveCount);
            Assert.Equal(reminder.Id, _repository.Stored.Single().Id);
            Assert.Equal(reminder.Id, _view.Shown.Single().Id);
        }

        [Fact]
        public void DeletingAReminder_RemovesIt_AndSaves()
        {
            var reminder = Reminder.Create("Su iç", DateTime.Now.AddMinutes(20));
            _repository.Stored.Add(reminder);
            Controller();

            _view.Delete(reminder.Id);

            Assert.Empty(_view.Shown);
            Assert.Empty(_repository.Stored);
        }

        [Fact]
        public void DeletingAnUnknownReminder_ChangesNothing()
        {
            Controller();

            _view.Delete(Guid.NewGuid());

            Assert.Equal(0, _repository.SaveCount);
        }

        [Fact]
        public void WhenAReminderIsDue_ANotificationComes_TheSpiderBringsTheNote_AndItLeavesTheList()
        {
            DateTime now = DateTime.Now;
            _repository.Stored.Add(Reminder.Create("Su iç", now.AddMinutes(5)));
            ReminderSessionController controller = Controller();

            controller.Tick(now.AddMinutes(5));

            Assert.Equal(new[] { "Su iç" }, _notifications.Messages);
            Assert.Equal(1, _engine.Spider.Messages.PendingNoteCount);
            Assert.Empty(_view.Shown);
            Assert.Empty(_repository.Stored);
        }

        [Fact]
        public void WhenTheDayChanges_TheListIsRebuilt_SoTomorrowBecomesToday()
        {
            DateTime evening = DateTime.Today.AddHours(23);
            _repository.Stored.Add(Reminder.Create("Faturayı öde", DateTime.Today.AddDays(1).AddHours(9)));
            ReminderSessionController controller = Controller();
            controller.Tick(evening);
            int shownBefore = _view.ShowCount;

            controller.Tick(DateTime.Today.AddDays(1).AddMinutes(1));

            Assert.True(_view.ShowCount > shownBefore);
            Assert.StartsWith("Bugün", _view.Shown.Single().When);
        }
    }
}
