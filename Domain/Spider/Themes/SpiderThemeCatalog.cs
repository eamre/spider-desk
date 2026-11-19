using System.Collections.Generic;
using System.Linq;

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

        /// <summary>Ayar dosyasında tanınmayan bir tema adı varsa bu tema kullanılır.</summary>
        public static ISpiderTheme Default { get; } = All.OfType<ForestTarantulaTheme>().Single();

        public static ISpiderTheme GetById(string? id)
        {
            return All.FirstOrDefault(t => t.Id == id) ?? Default;
        }
    }
}
