using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Tests.Domain.Web
{
    public class WebDropletControllerTests
    {
        private static readonly Point Center = new(100, 100);

        private static WebSegment Thread(double y) => new(new Point(50, y), new Point(150, y), IsAnchor: false);
        private static WebSegment AnchorThread(double y) => new(new Point(50, y), new Point(150, y), IsAnchor: true);

        // ---------- Damla ekleme ----------

        [Fact]
        public void RainBurst_NeverPutsDropletsOnAnchorThreads()
        {
            var segments = new List<WebSegment> { AnchorThread(80), Thread(100), AnchorThread(120) };
            var droplets = new WebDropletController();

            droplets.AddRainBurst(segments, new Random(7), count: 10);

            Assert.NotEmpty(droplets.AttachedDroplets);
            Assert.All(droplets.AttachedDroplets, d => Assert.Equal(1, d.SegmentIndex));
        }

        [Fact]
        public void RainBurst_StopsAtTheDropletLimit()
        {
            var droplets = new WebDropletController();

            droplets.AddRainBurst(new List<WebSegment> { Thread(100) }, new Random(1), count: 50);

            Assert.Equal(WebTuning.MaxAttachedDropletsPerWeb, droplets.AttachedDroplets.Count);
        }

        [Fact]
        public void RainBurst_OnAWebWithoutThreads_AddsNothing()
        {
            var droplets = new WebDropletController();

            droplets.AddRainBurst(new List<WebSegment>(), new Random(1), count: 5);

            Assert.Empty(droplets.AttachedDroplets);
        }

        // ---------- Ağırlık ve sarkma ----------

        [Fact]
        public void IsHeavyWithWater_TurnsTrueExactlyAtTheThreshold()
        {
            var segments = new List<WebSegment> { Thread(100) };
            var droplets = new WebDropletController();

            droplets.AddRainBurst(segments, new Random(1), count: WebTuning.HeavyWithWaterDropletCount - 1);
            Assert.False(droplets.IsHeavyWithWater);

            droplets.AddRainBurst(segments, new Random(2), count: 1);
            Assert.True(droplets.IsHeavyWithWater);
        }

        [Fact]
        public void SegmentSag_IsZeroWithoutDroplets_AndNeverExceedsTheLimit()
        {
            var segments = new List<WebSegment> { Thread(100), Thread(120) };
            var droplets = new WebDropletController();

            // Hepsi aynı tele gelsin diye yalnızca ilk teli veriyoruz
            droplets.AddRainBurst(new List<WebSegment> { segments[0] }, new Random(1), count: 50);

            Assert.Equal(WebTuning.MaxDropletSag, droplets.GetSegmentSag(0));
            Assert.Equal(0, droplets.GetSegmentSag(1));
        }

        // ---------- Silkeleme ve tel kopması ----------

        [Fact]
        public void ShakeOff_DropsEveryAttachedDroplet()
        {
            var segments = new List<WebSegment> { Thread(100), Thread(120) };
            var droplets = new WebDropletController();
            droplets.AddRainBurst(segments, new Random(3), count: 5);
            int revisionBefore = droplets.Revision;

            droplets.ShakeOffAllDroplets(segments, Center, new Random(3));

            Assert.Empty(droplets.AttachedDroplets);
            Assert.Equal(5, droplets.FallingDroplets.Count);
            Assert.All(droplets.FallingDroplets, d => Assert.True(d.IsFalling));
            Assert.True(droplets.Revision > revisionBefore);
        }

        [Fact]
        public void SegmentBroken_DropsItsDroplets_AndShiftsLaterDropletsDownByOne()
        {
            var segments = new List<WebSegment> { Thread(80), Thread(100), Thread(120) };
            var droplets = new WebDropletController();
            droplets.AddRainBurst(segments, new Random(5), count: 12);
            const int broken = 1;

            // Her damlanın kopmadan önceki tel numarasını not al
            var before = droplets.AttachedDroplets.ToDictionary(d => d, d => d.SegmentIndex);
            Assert.Contains(before.Values, i => i < broken);
            Assert.Contains(before.Values, i => i == broken);
            Assert.Contains(before.Values, i => i > broken);

            droplets.OnSegmentBroken(broken, segments, Center, new Random(5));

            foreach (var (drop, oldIndex) in before)
            {
                if (oldIndex == broken)
                {
                    Assert.True(drop.IsFalling);
                    Assert.Contains(drop, droplets.FallingDroplets);
                    Assert.DoesNotContain(drop, droplets.AttachedDroplets);
                }
                else
                {
                    // Kopan telden sonraki teller listede bir sıra yukarı kayar, damlalar da onlarla gider
                    int expected = oldIndex > broken ? oldIndex - 1 : oldIndex;
                    Assert.Equal(expected, drop.SegmentIndex);
                    Assert.Contains(drop, droplets.AttachedDroplets);
                }
            }
        }
    }
}
