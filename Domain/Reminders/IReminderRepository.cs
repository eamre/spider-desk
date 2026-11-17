using System.Collections.Generic;

namespace SpiderDesk.Domain.Reminders
{
    /// <summary>
    /// Hatırlatmaların uygulama kapanınca kaybolmaması için kalıcı olarak saklandığı yer.
    /// Domain yalnızca bu sözleşmeyi bilir; dosyaya nasıl yazıldığı Infrastructure katmanının işidir.
    /// </summary>
    public interface IReminderRepository
    {
        /// <summary>Kaydedilmiş hatırlatmaları okur. Hiç yoksa ya da okunamazsa boş liste döner.</summary>
        IReadOnlyList<Reminder> Load();

        /// <summary>Verilen hatırlatmaları kaydeder (öncekilerin yerine). Başarılıysa true döner.</summary>
        bool Save(IEnumerable<Reminder> reminders);
    }
}
