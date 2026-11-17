using SpiderDesk.Domain.Performance;

namespace SpiderDesk.Tests.Domain.Performance
{
    /// <summary>
    /// Duyuru ya da not gösterilirken katmanın öne çıkma sırasını doğrular:
    /// önce sahne hazırlanır (ağlar gizlenir, her şey yeniden çizilir), katman bir kare sonra öne çıkar.
    /// </summary>
    public class AnnouncementCurtainTests
    {
        private readonly AnnouncementCurtain _curtain = new();

        private CurtainStep Announcing(bool overAnApp = true) =>
            _curtain.Next(isShowingAnnouncement: true, isPaused: false, isDesktopCovered: overAnApp);

        private CurtainStep Quiet(bool overAnApp = true) =>
            _curtain.Next(isShowingAnnouncement: false, isPaused: false, isDesktopCovered: overAnApp);

        private CurtainStep AnnouncingWhilePaused() =>
            _curtain.Next(isShowingAnnouncement: true, isPaused: true, isDesktopCovered: true);

        private static readonly CurtainStep Nothing = new(HideBackground: false, RedrawEverything: false, RaiseToTop: false);

        [Fact]
        public void WithoutAnAnnouncement_NothingHappens()
        {
            Assert.Equal(Nothing, Quiet());
            Assert.Equal(Nothing, Quiet(overAnApp: false));
        }

        [Fact]
        public void FirstFrame_PreparesTheStage_ButStaysBehindTheApp()
        {
            CurtainStep first = Announcing();

            Assert.True(first.HideBackground);
            Assert.True(first.RedrawEverything);
            Assert.False(first.RaiseToTop);
        }

        [Fact]
        public void SecondFrame_RaisesTheCurtain_WithoutRedrawingEverythingAgain()
        {
            Announcing();

            CurtainStep second = Announcing();

            Assert.True(second.RaiseToTop);
            Assert.False(second.RedrawEverything);
            Assert.True(second.HideBackground);
        }

        [Fact]
        public void StaysInFront_ForAsLongAsTheAnnouncementLasts()
        {
            Announcing();
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(new CurtainStep(HideBackground: true, RedrawEverything: false, RaiseToTop: true), Announcing());
            }
        }

        [Fact]
        public void OnTheDesktop_WebsStayVisible()
        {
            CurtainStep first = Announcing(overAnApp: false);
            CurtainStep second = Announcing(overAnApp: false);

            Assert.False(first.HideBackground);
            Assert.False(second.HideBackground);
            Assert.True(second.RaiseToTop);
        }

        [Fact]
        public void WhenTheAnnouncementEnds_GoesBackAndShowsTheWebsAgain()
        {
            Announcing();
            Announcing();

            Assert.Equal(Nothing, Quiet());
        }

        [Fact]
        public void WhilePaused_NeverComesToTheFront()
        {
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(Nothing, AnnouncingWhilePaused());
            }
        }

        [Fact]
        public void PausingDuringAnAnnouncement_SendsItBackAtOnce()
        {
            Announcing();
            Announcing();

            Assert.Equal(Nothing, AnnouncingWhilePaused());
        }

        [Fact]
        public void ResumingDuringAnAnnouncement_PreparesTheStageAgainBeforeRising()
        {
            Announcing();
            Announcing();
            AnnouncingWhilePaused();

            CurtainStep first = Announcing();
            CurtainStep second = Announcing();

            Assert.True(first.RedrawEverything);
            Assert.False(first.RaiseToTop);
            Assert.True(second.RaiseToTop);
        }

        [Fact]
        public void ANewAnnouncementRightAfterAnother_StillWaitsOneFrame()
        {
            Announcing();
            Announcing();
            Quiet();

            CurtainStep next = Announcing();

            Assert.True(next.RedrawEverything);
            Assert.False(next.RaiseToTop);
        }
    }
}
