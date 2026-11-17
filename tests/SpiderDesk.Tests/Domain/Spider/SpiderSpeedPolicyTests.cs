using SpiderDesk.Core.Settings;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Tests.Domain.Spider
{
    public class SpiderSpeedPolicyTests
    {
        [Theory]
        [InlineData(SpiderSpeedPreset.Calm, SpiderSpeedPolicy.CalmMultiplier)]
        [InlineData(SpiderSpeedPreset.Normal, SpiderSpeedPolicy.NormalMultiplier)]
        [InlineData(SpiderSpeedPreset.Agile, SpiderSpeedPolicy.AgileMultiplier)]
        public void MultiplierFor_ReturnsTheMultiplierOfEachPreset(SpiderSpeedPreset preset, double expected)
        {
            Assert.Equal(expected, SpiderSpeedPolicy.MultiplierFor(preset));
        }

        [Fact]
        public void MultiplierFor_UnknownPreset_FallsBackToNormal()
        {
            // Örneğin ayar dosyası elle bozulup tanınmayan bir sayı yazılırsa
            var unknown = (SpiderSpeedPreset)99;

            Assert.Equal(SpiderSpeedPolicy.NormalMultiplier, SpiderSpeedPolicy.MultiplierFor(unknown));
        }

        [Fact]
        public void Presets_GetFasterFromCalmToAgile()
        {
            Assert.True(SpiderSpeedPolicy.CalmMultiplier < SpiderSpeedPolicy.NormalMultiplier);
            Assert.True(SpiderSpeedPolicy.NormalMultiplier < SpiderSpeedPolicy.AgileMultiplier);
        }
    }
}
