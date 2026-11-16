using SpiderDesk.Core.Settings;

namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Kullanıcının seçtiği hız ayarını örümceğin bütün hareketlerine uygulanan çarpana çevirir.
    /// </summary>
    public static class SpiderSpeedPolicy
    {
        public const double CalmMultiplier = 0.75;
        public const double NormalMultiplier = 1.0;
        public const double AgileMultiplier = 1.35;

        public static double MultiplierFor(SpiderSpeedPreset preset) => preset switch
        {
            SpiderSpeedPreset.Calm => CalmMultiplier,
            SpiderSpeedPreset.Agile => AgileMultiplier,
            _ => NormalMultiplier
        };
    }
}
