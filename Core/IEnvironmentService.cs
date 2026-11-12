using System;

namespace SpiderDesk.Core
{
    public enum EnvironmentTimeMode
    {
        AutoBySystemClock,
        AlwaysDay,
        AlwaysNight
    }

    /// <summary>
    /// Gece/Gündüz döngüsü ve yerel saat bilgisini sağlayan servis arayüzü.
    /// </summary>
    public interface IEnvironmentService
    {
        EnvironmentTimeMode TimeMode { get; set; }
        bool IsNightTime { get; }

        /// <summary>Sistem saatine göre günün yerel saati (gece/gündüz tercihinden bağımsızdır).</summary>
        TimeSpan LocalTimeOfDay { get; }
    }
}
