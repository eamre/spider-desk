using System.Collections.Generic;
using System.Linq;
using SpiderDesk.Domain.Reminders;

namespace SpiderDesk.Infrastructure.Storage
{
    /// <summary>
    /// Bekleyen hatırlatmaları reminders.json dosyasında kalıcı tutar.
    /// Dosya elle bozulmuşsa okunabilen hatırlatmalar yüklenir, metni boş ya da geçersiz olanlar atlanır.
    /// </summary>
    public sealed class JsonReminderRepository : IReminderRepository
    {
        private readonly JsonFile<List<Reminder?>> _file;

        public JsonReminderRepository()
            : this(AppDataFolder.PathOf("reminders.json"))
        {
        }

        /// <summary>Testler gerçek hatırlatma dosyasına dokunmamak için geçici bir dosya yolu verir.</summary>
        public JsonReminderRepository(string filePath)
        {
            _file = new JsonFile<List<Reminder?>>(filePath);
        }

        public IReadOnlyList<Reminder> Load()
        {
            List<Reminder?> saved = _file.Read() ?? new List<Reminder?>();
            return saved
                .OfType<Reminder>()
                .Where(reminder => Reminder.IsValidText(reminder.Text))
                .ToList();
        }

        public bool Save(IEnumerable<Reminder> reminders) => _file.Write(reminders.Cast<Reminder?>().ToList());
    }
}
