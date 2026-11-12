using System.Collections.Generic;
using System.Linq;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Spider.Themes
{
    public static class SpiderThemeCatalog
    {
        public static IReadOnlyList<ISpiderTheme> All { get; } =
        [
            new BlackWidowTheme(),
            new FrostCrystalTheme(),
            new ForestTarantulaTheme(),
            new CyberNeonTheme()
        ];

        public static ISpiderTheme GetById(string? id)
        {
            return All.FirstOrDefault(t => t.Id == id) ?? All[0];
        }
    }
}
