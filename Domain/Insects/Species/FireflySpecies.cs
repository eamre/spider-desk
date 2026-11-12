using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Insects.Species
{
    /// <summary>
    /// Ateş Böceği (Firefly): Etrafına sarı-yeşil biyolüminesans ışık saçarak uçar.
    /// </summary>
    public sealed class FireflySpecies : IBugSpecies
    {
        public string Name => "Ateş Böceği";
        public Color BodyColor => Color.FromRgb(70, 90, 45);
        public Color? GlowColor => Color.FromRgb(195, 255, 75);
        public double BaseSize => 5.6;
        public double MinSpeed => 85.0;
        public double MaxSpeed => 150.0;
        public int WebDamageOnImpact => 0;
        public double EscapeChance => 0.0;
        public bool CanPerchOnDesktop => true;
    }
}
