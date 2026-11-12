using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Insects.Species
{
    public sealed class GoldenMothSpecies : IBugSpecies
    {
        public string Name => "Altın Güve";
        public Color BodyColor => Color.FromRgb(190, 145, 55);
        public Color? GlowColor => null;
        public double BaseSize => 6.4;
        public double MinSpeed => 95.0;
        public double MaxSpeed => 165.0;
        public int WebDamageOnImpact => 0;
        public double EscapeChance => 0.0;
        public bool CanPerchOnDesktop => true;
    }
}
