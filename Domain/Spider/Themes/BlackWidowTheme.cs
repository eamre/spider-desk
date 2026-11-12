using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Spider.Themes
{
    public sealed class BlackWidowTheme : ISpiderTheme
    {
        public string Id => "black_widow";
        public string DisplayName => "🕷️ Karadul (Klasik)";
        public Color LegUpperColor => Color.FromRgb(28, 26, 30);
        public Color LegLowerColor => Color.FromRgb(52, 46, 50);
        public Color JointColor => Color.FromRgb(40, 35, 38);
        public Color AbdomenColor => Color.FromRgb(22, 20, 24);
        public Color ThoraxColor => Color.FromRgb(35, 31, 36);
        public Color MarkingColor => Color.FromRgb(210, 45, 45);
        public Color EyeColor => Color.FromRgb(255, 70, 70);
        public Color WebAnchorColor => Color.FromArgb(185, 240, 248, 255);
        public Color WebSpiralColor => Color.FromArgb(135, 230, 242, 255);
    }
}
