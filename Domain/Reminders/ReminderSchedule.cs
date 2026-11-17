using System;
using System.Collections.Generic;

namespace SpiderDesk.Domain.Reminders
{
    /// <summary>
    /// Bekleyen hatırlatmaların listesi. Hatırlatmaları her zaman zamanına göre sıralı tutar.
    /// Saat dışarıdan verilir (now); böylece testlerde gerçek saati beklemeden her durum denenebilir.
    /// </summary>
    public sealed class ReminderSchedule
    {
        private readonly List<Reminder> _reminders = new();

        /// <summary>Bekleyen hatırlatmalar, en yakın olan başta.</summary>
        public IReadOnlyList<Reminder> Upcoming => _reminders;

        public ReminderSchedule(IEnumerable<Reminder>? savedReminders = null)
        {
            if (savedReminders == null) return;

            foreach (var reminder in savedReminders)
            {
                Add(reminder);
            }
        }

        public void Add(Reminder reminder)
        {
            // Sırayı korumak için, kendisinden sonra gelecek ilk hatırlatmanın önüne yerleştirilir.
            // Aynı saatteki hatırlatmalar eklendikleri sırayla kalır.
            int index = _reminders.FindIndex(existing => existing.DueAt > reminder.DueAt);
            _reminders.Insert(index < 0 ? _reminders.Count : index, reminder);
        }

        /// <summary>Hatırlatmayı listeden siler. Bulunamazsa (örneğin zaten gösterilmişse) false döner.</summary>
        public bool Remove(Guid id) => _reminders.RemoveAll(reminder => reminder.Id == id) > 0;

        /// <summary>
        /// Zamanı gelmiş (ya da geçmiş) hatırlatmaları listeden çıkarır ve en eskisi başta olacak şekilde verir.
        /// Bilgisayar uykudan uyanınca ya da uygulama açılınca birden fazla hatırlatma birlikte gelebilir.
        /// </summary>
        public IReadOnlyList<Reminder> TakeDue(DateTime now)
        {
            int dueCount = 0;
            while (dueCount < _reminders.Count && _reminders[dueCount].DueAt <= now)
            {
                dueCount++;
            }

            if (dueCount == 0) return Array.Empty<Reminder>();

            var due = _reminders.GetRange(0, dueCount);
            _reminders.RemoveRange(0, dueCount);
            return due;
        }
    }
}
