using SpiderDesk.Infrastructure.Flyout;

namespace SpiderDesk.Tests.Infrastructure.Flyout
{
    /// <summary>
    /// Tepsi ikonuna tıklamanın pencereyi ne zaman açtığını doğrular. Asıl kural: pencere açıkken ikona basmak onu
    /// önce dışarı tıklanmış gibi gizler; aynı tıklama pencereyi yeniden açmamalı, yoksa pencere kapanıp açılarak titrer.
    /// </summary>
    public class FlyoutToggleTests
    {
        private static readonly DateTime Now = new(2026, 11, 18, 21, 30, 0, DateTimeKind.Utc);

        [Fact]
        public void AHiddenWindow_ThatWasNeverAutoHidden_Opens()
        {
            Assert.True(FlyoutToggle.ShouldOpen(isVisible: false, lastAutoHiddenAt: null, Now));
        }

        [Fact]
        public void AnOpenWindow_DoesNotOpenAgain()
        {
            Assert.False(FlyoutToggle.ShouldOpen(isVisible: true, lastAutoHiddenAt: null, Now));
        }

        [Fact]
        public void RightAfterClickingOutside_TheSameClickOnTheIcon_DoesNotReopenIt()
        {
            Assert.False(FlyoutToggle.ShouldOpen(false, Now.AddMilliseconds(-150), Now));
        }

        [Fact]
        public void ALaterClickOnTheIcon_OpensItAgain()
        {
            Assert.True(FlyoutToggle.ShouldOpen(false, Now - FlyoutToggle.SameClickWindow, Now));
            Assert.True(FlyoutToggle.ShouldOpen(false, Now.AddSeconds(-5), Now));
        }
    }
}
