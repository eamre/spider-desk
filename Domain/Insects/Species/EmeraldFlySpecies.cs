using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Insects.Species
{
    public sealed class EmeraldFlySpecies : IBugSpecies
    {
        public string Name => "Yeşil Sinek";
        public Color BodyColor => Color.FromRgb(30, 145, 95);
        public Color? GlowColor => null;
        public double BaseSize => 5.8;
        public double MinSpeed => 115.0;
        public double MaxSpeed => 200.0;
        public int WebDamageOnImpact => 0;
        public double EscapeChance => 0.18;
        public bool CanPerchOnDesktop => true;
    }
}
