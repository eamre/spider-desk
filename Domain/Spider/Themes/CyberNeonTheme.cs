using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Spider.Themes
{
    public sealed class CyberNeonTheme : ISpiderTheme
    {
        public string Id => "cyber_neon";
        public string DisplayName => "⚡ Siber Neon";
        public Color LegUpperColor => Color.FromRgb(45, 20, 75);
        public Color LegLowerColor => Color.FromRgb(115, 35, 165);
        public Color JointColor => Color.FromRgb(0, 245, 215);
        public Color AbdomenColor => Color.FromRgb(28, 14, 48);
        public Color ThoraxColor => Color.FromRgb(48, 24, 82);
        public Color MarkingColor => Color.FromRgb(0, 255, 220);
        public Color EyeColor => Color.FromRgb(255, 50, 220);
        public Color WebAnchorColor => Color.FromArgb(190, 0, 245, 225);
        public Color WebSpiralColor => Color.FromArgb(145, 210, 90, 255);
    }
}
