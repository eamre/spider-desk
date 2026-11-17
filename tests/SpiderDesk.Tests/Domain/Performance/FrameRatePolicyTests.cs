using SpiderDesk.Domain.Performance;

namespace SpiderDesk.Tests.Domain.Performance
{
    public class FrameRatePolicyTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Decide_WhenPaused_StopsEngineAndRenderingAtIdleRate(bool isOccluded, bool hasVisualActivity)
        {
            var decision = FrameRatePolicy.Decide(isPaused: true, isOccluded, hasVisualActivity);

            Assert.Equal(FrameRatePolicy.IdleFrameInterval, decision.TargetInterval);
            Assert.False(decision.ShouldUpdateEngine);
            Assert.False(decision.ShouldRenderCanvas);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Decide_WhenOccluded_KeepsSimulationRunningButSkipsRendering(bool hasVisualActivity)
        {
            var decision = FrameRatePolicy.Decide(isPaused: false, isOccluded: true, hasVisualActivity);

            Assert.Equal(FrameRatePolicy.IdleFrameInterval, decision.TargetInterval);
            Assert.True(decision.ShouldUpdateEngine);
            Assert.False(decision.ShouldRenderCanvas);
        }

        [Fact]
        public void Decide_WhenVisibleAndMoving_RunsAtActiveRate()
        {
            var decision = FrameRatePolicy.Decide(isPaused: false, isOccluded: false, hasVisualActivity: true);

            Assert.Equal(FrameRatePolicy.ActiveFrameInterval, decision.TargetInterval);
            Assert.True(decision.ShouldUpdateEngine);
            Assert.True(decision.ShouldRenderCanvas);
        }

        [Fact]
        public void Decide_WhenVisibleAndStill_KeepsRenderingAtIdleRate()
        {
            var decision = FrameRatePolicy.Decide(isPaused: false, isOccluded: false, hasVisualActivity: false);

            Assert.Equal(FrameRatePolicy.IdleFrameInterval, decision.TargetInterval);
            Assert.True(decision.ShouldUpdateEngine);
            Assert.True(decision.ShouldRenderCanvas);
        }

        [Fact]
        public void ActiveFrameInterval_IsShorterThanIdleFrameInterval()
        {
            Assert.True(FrameRatePolicy.ActiveFrameInterval < FrameRatePolicy.IdleFrameInterval);
        }
    }
}
