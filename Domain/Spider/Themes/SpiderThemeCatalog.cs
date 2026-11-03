using System.Collections.Generic;
using System.Linq;
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

    public static class SpiderThemeCatalog
    {
        public static IReadOnlyList<ISpiderTheme> All { get; } = new ISpiderTheme[]
        {
            new BlackWidowTheme(),
            new FrostCrystalTheme(),
            new ForestTarantulaTheme(),
            new CyberNeonTheme()
        };

        public static ISpiderTheme GetById(string? id)
        {
            return All.FirstOrDefault(t => t.Id == id) ?? All[0];
        }
    }
}
