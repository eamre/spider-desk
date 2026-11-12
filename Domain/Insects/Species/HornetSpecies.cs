using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Insects.Species
{
    /// <summary>
    /// Güçlü İri Böcek / Yaban Arısı: Sadece bu tür ağa çarptığında telleri koparabilir ve ağı yırtıp kurtulabilir!
    /// </summary>
    public sealed class HornetSpecies : IBugSpecies
    {
        public string Name => "Yaban Arısı";
        public Color BodyColor => Color.FromRgb(215, 130, 20);
        public Color? GlowColor => null;
        public double BaseSize => 7.4;
        public double MinSpeed => 125.0;
        public double MaxSpeed => 215.0;
        public int WebDamageOnImpact => 2;
        public double EscapeChance => 0.35;
        public bool CanPerchOnDesktop => false;
    }
}
