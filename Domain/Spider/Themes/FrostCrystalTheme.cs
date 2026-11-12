using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Spider.Themes
{
    public sealed class FrostCrystalTheme : ISpiderTheme
    {
        public string Id => "frost_crystal";
        public string DisplayName => "❄️ Buz Kristali";
        public Color LegUpperColor => Color.FromRgb(65, 115, 155);
        public Color LegLowerColor => Color.FromRgb(120, 185, 225);
        public Color JointColor => Color.FromRgb(160, 220, 250);
        public Color AbdomenColor => Color.FromRgb(45, 85, 125);
        public Color ThoraxColor => Color.FromRgb(70, 125, 170);
        public Color MarkingColor => Color.FromRgb(135, 240, 255);
        public Color EyeColor => Color.FromRgb(200, 255, 255);
        public Color WebAnchorColor => Color.FromArgb(195, 185, 240, 255);
        public Color WebSpiralColor => Color.FromArgb(145, 160, 225, 255);
    }
}
