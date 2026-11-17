namespace SpiderDesk.Core.Services
{
    /// <summary>
    /// Kullanıcıya Windows bildirimi gösterir. Pomodoro ve hatırlatıcı yalnızca "bildirim göster" der;
    /// bildirimin tepsi ikonundan nasıl çıktığını bilmezler.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>Ekranın sağ altında bir bildirim gösterir; tam ekran bir uygulama açıkken de görünür.</summary>
        void ShowNotification(string title, string message);
    }
}
