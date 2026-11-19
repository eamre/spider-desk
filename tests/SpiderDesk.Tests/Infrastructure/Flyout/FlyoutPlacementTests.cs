using System.Windows;
using SpiderDesk.Infrastructure.Flyout;

namespace SpiderDesk.Tests.Infrastructure.Flyout
{
    /// <summary>
    /// Tepsiden açılan pencerenin, görev çubuğu hangi kenarda olursa olsun tepsinin olduğu köşede açıldığını doğrular.
    /// Ekran 1920×1080, görev çubuğu 40 piksel.
    /// </summary>
    public class FlyoutPlacementTests
    {
        private static readonly Rect Screen = new(0, 0, 1920, 1080);
        private static readonly Size Window = new(520, 640);
        private const double Gap = FlyoutPlacement.Gap;

        [Fact]
        public void TaskbarAtTheBottom_OpensBottomRight_JustAboveTheTaskbar()
        {
            var (topLeft, height) = FlyoutPlacement.Place(Screen, new Rect(0, 0, 1920, 1040), Window);

            Assert.Equal(1920 - 520 - Gap, topLeft.X);
            Assert.Equal(1040 - 640 - Gap, topLeft.Y);
            Assert.Equal(640, height);
        }

        [Fact]
        public void TaskbarAtTheTop_OpensTopRight_JustBelowTheTaskbar()
        {
            var (topLeft, _) = FlyoutPlacement.Place(Screen, new Rect(0, 40, 1920, 1040), Window);

            Assert.Equal(1920 - 520 - Gap, topLeft.X);
            Assert.Equal(40 + Gap, topLeft.Y);
        }

        [Fact]
        public void TaskbarOnTheRight_OpensBottomRight_NextToTheTaskbar()
        {
            var (topLeft, _) = FlyoutPlacement.Place(Screen, new Rect(0, 0, 1880, 1080), Window);

            Assert.Equal(1880 - 520 - Gap, topLeft.X);
            Assert.Equal(1080 - 640 - Gap, topLeft.Y);
        }

        [Fact]
        public void TaskbarOnTheLeft_OpensBottomLeft_NextToTheTaskbar()
        {
            var (topLeft, _) = FlyoutPlacement.Place(Screen, new Rect(40, 0, 1880, 1080), Window);

            Assert.Equal(40 + Gap, topLeft.X);
            Assert.Equal(1080 - 640 - Gap, topLeft.Y);
        }

        [Fact]
        public void HiddenTaskbar_OpensBottomRight()
        {
            var (topLeft, _) = FlyoutPlacement.Place(Screen, Screen, Window);

            Assert.Equal(1920 - 520 - Gap, topLeft.X);
            Assert.Equal(1080 - 640 - Gap, topLeft.Y);
        }

        [Fact]
        public void OnALowScreen_TheWindowIsShortenedToFit()
        {
            var lowScreen = new Rect(0, 0, 1366, 600);
            var (topLeft, height) = FlyoutPlacement.Place(lowScreen, new Rect(0, 0, 1366, 560), Window);

            Assert.Equal(560 - 2 * Gap, height);
            Assert.Equal(Gap, topLeft.Y);
        }
    }
}
