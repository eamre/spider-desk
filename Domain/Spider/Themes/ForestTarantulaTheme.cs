using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Spider.Themes
{
    public sealed class ForestTarantulaTheme : ISpiderTheme
    {
        public string Id => "forest_tarantula";
        public string DisplayName => "🌿 Orman Tarantulası";
        public Color LegUpperColor => Color.FromRgb(75, 48, 28);
        public Color LegLowerColor => Color.FromRgb(125, 82, 45);
        public Color JointColor => Color.FromRgb(165, 110, 55);
        public Color AbdomenColor => Color.FromRgb(58, 38, 22);
        public Color ThoraxColor => Color.FromRgb(82, 54, 32);
        public Color MarkingColor => Color.FromRgb(235, 165, 50);
        public Color EyeColor => Color.FromRgb(255, 200, 70);
        public Color WebAnchorColor => Color.FromArgb(185, 255, 248, 225);
        public Color WebSpiralColor => Color.FromArgb(135, 245, 235, 205);
    }
}
