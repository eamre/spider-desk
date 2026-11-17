using System;
using System.Collections.Generic;
using System.Linq;
using SpiderDesk.Core.Services;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Reminders;
using SpiderDesk.Infrastructure.Dialogs;
using SpiderDesk.Infrastructure.Tray;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Hatırlatıcıyı tepsi menüsüne, ekleme penceresine, örümceğe ve Windows bildirimlerine bağlar.
    /// Her karede <see cref="Tick"/> ile o anki saat verilir; zamanı gelen hatırlatma için bildirim gelir
    /// ve örümcek notu tavandan sarkarak gösterir. Her değişiklik reminders.json dosyasına hemen kaydedilir.
    /// </summary>
    public sealed class ReminderSessionController
    {
        private readonly ReminderSchedule _schedule;
        private readonly IReminderRepository _repository;
        private readonly SimulationEngine _engine;
        private readonly ReminderMenu _menu;
        private readonly INotificationService _notifications;
        private ReminderWindow? _openWindow;

        // Listedeki "Bugün" / "Yarın" yazıları gün değişince eskir; o zaman liste yeniden kurulur
        private DateTime _listShownForDate;

        public ReminderSessionController(
            SimulationEngine engine,
            IReminderRepository repository,
            ReminderMenu menu,
            INotificationService notifications)
        {
            _engine = engine;
            _repository = repository;
            _menu = menu;
            _notifications = notifications;
            _schedule = new ReminderSchedule(repository.Load());

            _menu.AddRequested += OpenAddWindow;
            _menu.DeleteRequested += Delete;

            ShowList(DateTime.Now);
        }

        /// <summary>
        /// Kare döngüsünden çağrılır. Uygulama duraklatılmış olsa bile çağrılmalıdır ki hatırlatmalar kaçmasın.
        /// Uygulama açılır açılmaz ilk çağrıda, kapalıyken zamanı geçmiş hatırlatmalar da ("Kaçırdın") gelir.
        /// </summary>
        public void Tick(DateTime now)
        {
            IReadOnlyList<Reminder> due = _schedule.TakeDue(now);
            if (due.Count > 0)
            {
                Save();
                Announce(due, now);
                ShowList(now);
            }
            else if (now.Date != _listShownForDate)
            {
                ShowList(now);
            }
        }

        private void Announce(IReadOnlyList<Reminder> due, DateTime now)
        {
            ReminderNotification notification = ReminderText.Notification(due, now);
            _notifications.ShowNotification(notification.Title, notification.Message);

            foreach (Reminder reminder in due)
            {
                _engine.RequestNote(ReminderText.NoteText(reminder, now));
            }
        }

        private void OpenAddWindow()
        {
            // Pencere zaten açıksa ikincisini açmak yerine onu öne getir
            if (_openWindow != null)
            {
                _openWindow.Activate();
                return;
            }

            _openWindow = new ReminderWindow();
            _openWindow.ReminderCreated += Add;
            _openWindow.Closed += (_, _) => _openWindow = null;
            _openWindow.Show();
            _openWindow.Activate();
        }

        private void Add(Reminder reminder)
        {
            _schedule.Add(reminder);
            Save();
            ShowList(DateTime.Now);
        }

        private void Delete(Guid id)
        {
            if (_schedule.Remove(id))
            {
                Save();
                ShowList(DateTime.Now);
            }
        }

        private void Save() => _repository.Save(_schedule.Upcoming);

        private void ShowList(DateTime now)
        {
            _listShownForDate = now.Date;
            _menu.ShowReminders(
                ReminderText.MenuTitleFor(_schedule.Upcoming.Count),
                _schedule.Upcoming.Select(reminder => (reminder.Id, ReminderText.ListLabel(reminder, now))).ToList());
        }
    }
}
