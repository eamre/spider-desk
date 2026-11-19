using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Spider.Themes;

namespace SpiderDesk.Tests.Domain.Spider.Themes
{
    /// <summary>
    /// Hangi örümcek temasının görüneceğini doğrular: uygulamayı ilk kez açan kişi Orman Tarantulası'nı görür,
    /// seçilen tema adıyla bulunur ve ayar dosyasındaki tanınmayan bir ad da Orman Tarantulası'na döner.
    /// </summary>
    public class SpiderThemeCatalogTests
    {
        [Fact]
        public void SomeoneOpeningTheAppForTheFirstTime_SeesTheForestTarantula()
        {
            ISpiderTheme theme = SpiderThemeCatalog.GetById(new UserSettings().ThemeId);

            Assert.IsType<ForestTarantulaTheme>(theme);
        }

        [Theory]
        [InlineData("black_widow", typeof(BlackWidowTheme))]
        [InlineData("frost_crystal", typeof(FrostCrystalTheme))]
        [InlineData("forest_tarantula", typeof(ForestTarantulaTheme))]
        [InlineData("cyber_neon", typeof(CyberNeonTheme))]
        public void AChosenTheme_IsFoundByItsName(string id, Type expected)
        {
            Assert.IsType(expected, SpiderThemeCatalog.GetById(id));
        }

        // Örneğin ayar dosyası elle bozulmuşsa ya da artık olmayan bir tema kayıtlıysa
        [Theory]
        [InlineData("no_such_theme")]
        [InlineData("")]
        [InlineData(null)]
        public void AnUnknownThemeName_FallsBackToTheForestTarantula(string? id)
        {
            Assert.IsType<ForestTarantulaTheme>(SpiderThemeCatalog.GetById(id));
        }
    }
}
