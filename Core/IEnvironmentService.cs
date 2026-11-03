namespace SpiderDesk.Core
{
    public enum EnvironmentTimeMode
    {
        AutoBySystemClock,
        AlwaysDay,
        AlwaysNight
    }

    /// <summary>
    /// Gece/Gündüz döngüsü bilgisini sağlayan servis arayüzü (ISP & DIP).
    /// </summary>
    public interface IEnvironmentService
    {
        EnvironmentTimeMode TimeMode { get; set; }
        bool IsNightTime { get; }
    }
}
