using System;
using System.Collections.Generic;
using SpiderDesk.Domain.Reminders;

namespace SpiderDesk.Infrastructure.Sessions
{
    /// <summary>Listede gösterilen bir hatırlatma: ne zaman ("Bugün 14:30") ve ne.</summary>
    public readonly record struct ReminderListItem(Guid Id, string When, string Text);

    /// <summary>
    /// Hatırlatmaların listelendiği, eklendiği ve silindiği yer (ayarlar penceresinin "Hatırlatıcılar" sekmesi).
    /// <see cref="ReminderSessionController"/> yalnızca bu arayüzü tanır; testlerde yerine sahte bir görünüm verilir.
    /// </summary>
    public interface IReminderView
    {
        /// <summary>Kullanıcı yeni bir hatırlatmayı kaydetti.</summary>
        event Action<Reminder>? ReminderCreated;

        /// <summary>Kullanıcı bir hatırlatmayı sildi.</summary>
        event Action<Guid>? DeleteRequested;

        /// <summary>Bekleyen hatırlatmaları, en yakını en üstte olacak şekilde gösterir.</summary>
        void ShowReminders(IReadOnlyList<ReminderListItem> reminders);
    }
}
