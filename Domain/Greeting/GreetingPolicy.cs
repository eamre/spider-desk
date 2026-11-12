using System;

namespace SpiderDesk.Domain.Greeting
{
    /// <summary>
    /// Günün saatine göre örümceğin açılışta ipekle yazacağı selamı seçen saf kural sınıfı (Pure Policy).
    /// Saat dışarıdan verildiği için hiçbir dış bağımlılığı yoktur ve kolayca test edilebilir.
    /// </summary>
    public static class GreetingPolicy
    {
        public const string Morning = "GOOD MORNING";
        public const string Day = "HELLO";
        public const string Evening = "GOOD EVENING";
        public const string Night = "GOOD NIGHT";

        public static string ChooseGreeting(TimeSpan timeOfDay) => timeOfDay.Hours switch
        {
            >= 5 and < 12 => Morning,
            >= 12 and < 18 => Day,
            >= 18 and < 22 => Evening,
            _ => Night
        };
    }
}
