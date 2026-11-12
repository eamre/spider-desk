using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Insects.Species
{
    public sealed class HouseFlySpecies : IBugSpecies
    {
        public string Name => "Karasinek";
        public Color BodyColor => Color.FromRgb(45, 55, 65);
        public Color? GlowColor => null;
        public double BaseSize => 5.4;
        public double MinSpeed => 105.0;
        public double MaxSpeed => 185.0;
        public int WebDamageOnImpact => 0;
        public double EscapeChance => 0.0;
        public bool CanPerchOnDesktop => true;
    }
}
