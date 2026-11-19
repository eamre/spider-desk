namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>Tepsi ikonunun üzerine gelince görünen ipucu: Pomodoro çalışırken pencereyi açmadan geri sayımı gösterir.</summary>
    public interface ITrayTooltip
    {
        /// <summary>Örneğin "Odak 18:32". Null ise ipucu uygulamanın adına döner.</summary>
        void ShowPomodoroCountdown(string? countdown);
    }
}
