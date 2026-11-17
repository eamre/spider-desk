using System.Collections.Generic;
using SpiderDesk.Domain.Spider.States;

namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Örümceğin göstermesi için sırada bekleyen mesajlar: ipekle yazılacak FOCUS/BREAK ve tavandan gösterilecek notlar.
    /// Örümcek ne zaman göstereceğine kendisi karar verir (ancak işini bırakabildiğinde); bu sınıf yalnızca sırayı tutar.
    /// </summary>
    public sealed class SpiderMessageQueue
    {
        private string? _pendingSilkMessage;
        private readonly Queue<string> _pendingNotes = new();

        public bool HasPendingSilkMessage => _pendingSilkMessage != null;
        public int PendingNoteCount => _pendingNotes.Count;

        /// <summary>
        /// İpekle yazılacak bir mesaj verir (örneğin Pomodoro'nun FOCUS / BREAK duyurusu).
        /// Henüz yazılmamış eski bir mesaj varsa yenisi onun yerini alır; çünkü yalnızca son tur önemlidir.
        /// </summary>
        public void RequestSilkMessage(string text) => _pendingSilkMessage = text;

        /// <summary>Henüz yazılmaya başlanmamış mesajı iptal eder. Yazılmakta olan mesaj yarıda kesilmez.</summary>
        public void CancelPendingSilkMessage() => _pendingSilkMessage = null;

        /// <summary>
        /// Tavandan sarkarak gösterilecek bir not verir (örneğin bir hatırlatma).
        /// Birden fazla not gelirse hiçbiri kaybolmaz, geldikleri sırayla gösterilir.
        /// </summary>
        public void RequestNote(string text) => _pendingNotes.Enqueue(text);

        /// <summary>
        /// Sıradaki mesajı sıradan çıkarıp onu gösterecek durumu verir: önce FOCUS/BREAK yazısı,
        /// yoksa sıradaki not. Bekleyen mesaj yoksa null döner.
        /// </summary>
        public ISpiderBehaviorState? TakeNextMessageState()
        {
            if (_pendingSilkMessage is { } message)
            {
                _pendingSilkMessage = null;
                return WritingSilkMessageState.Announcement(message);
            }

            if (_pendingNotes.Count > 0)
            {
                return new DeliveringNoteState(_pendingNotes.Dequeue());
            }

            return null;
        }
    }
}
