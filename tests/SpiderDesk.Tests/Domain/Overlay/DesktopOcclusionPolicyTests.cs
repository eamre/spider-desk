using SpiderDesk.Domain.Overlay;

namespace SpiderDesk.Tests.Domain.Overlay
{
    public class DesktopOcclusionPolicyTests
    {
        // Her test yalnızca kendi konusuyla ilgili alanı değiştirir; geri kalanı sıradan, görünür bir pencere olarak kalır
        private static WindowSnapshot Window(
            bool isDesktop = false,
            bool isMinimized = false,
            bool isMaximized = false,
            bool coversMonitor = false,
            bool isOnOverlayMonitor = true) =>
            new(isDesktop, isMinimized, isMaximized, coversMonitor, isOnOverlayMonitor);

        [Fact]
        public void Occludes_DefaultSnapshot_IsFalse()
        {
            Assert.False(DesktopOcclusionPolicy.Occludes(default));
        }

        [Fact]
        public void Occludes_NormalWindow_IsFalse()
        {
            Assert.False(DesktopOcclusionPolicy.Occludes(Window()));
        }

        [Fact]
        public void Occludes_MaximizedWindow_IsTrue()
        {
            Assert.True(DesktopOcclusionPolicy.Occludes(Window(isMaximized: true)));
        }

        [Fact]
        public void Occludes_BorderlessFullscreenWindow_IsTrue()
        {
            Assert.True(DesktopOcclusionPolicy.Occludes(Window(coversMonitor: true)));
        }

        [Fact]
        public void Occludes_DesktopItself_IsFalse()
        {
            Assert.False(DesktopOcclusionPolicy.Occludes(Window(isDesktop: true, coversMonitor: true)));
        }

        [Fact]
        public void Occludes_MinimizedMaximizedWindow_IsFalse()
        {
            Assert.False(DesktopOcclusionPolicy.Occludes(Window(isMinimized: true, isMaximized: true)));
        }

        [Fact]
        public void Occludes_MaximizedWindowOnAnotherMonitor_IsFalse()
        {
            Assert.False(DesktopOcclusionPolicy.Occludes(Window(isMaximized: true, isOnOverlayMonitor: false)));
        }
    }
}
