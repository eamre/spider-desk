using System;
using SpiderDesk.Core;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Sistem saatine veya kullanıcı tercihine göre Gece/Gündüz durumunu belirler (SRP & DIP).
    /// </summary>
    public class SystemEnvironmentService : IEnvironmentService
    {
        public EnvironmentTimeMode TimeMode { get; set; } = EnvironmentTimeMode.AutoBySystemClock;

        public bool IsNightTime
        {
            get
            {
                return TimeMode switch
                {
                    EnvironmentTimeMode.AlwaysDay => false,
                    EnvironmentTimeMode.AlwaysNight => true,
                    _ => IsSystemClockNight()
                };
            }
        }

        private static bool IsSystemClockNight()
        {
            int hour = DateTime.Now.Hour;
            return hour >= 19 || hour < 6;
        }
    }
}
