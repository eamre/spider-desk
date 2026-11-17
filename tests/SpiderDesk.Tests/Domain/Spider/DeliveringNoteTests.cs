using System;
using System.Linq;
using System.Windows;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Spider.States;

namespace SpiderDesk.Tests.Domain.Spider
{
    /// <summary>
    /// Örümceğin tavandan sarkıp notu ön bacaklarıyla tabela gibi gösterdiği durumu doğrular.
    /// </summary>
    public class DeliveringNoteTests
    {
        private const double FrameSeconds = 0.02;

        private static (SpiderEntity Spider, WorldContext World) StartDelivery(string text = "Annemi ara")
        {
            var world = new WorldContext();
            var spider = new SpiderEntity(new Point(900, 700), new SpiderGaitController());
            spider.TransitionTo(new DeliveringNoteState(text), world);
            return (spider, world);
        }

        // Kart tamamen görünür olup örümcek sarkma yüksekliğine inene kadar ilerletir
        private static double RunUntilHolding(SpiderEntity spider, WorldContext world)
        {
            double holdY = world.ScreenHeight * SpiderTuning.NoteHangHeightRatio;
            double elapsed = 0;
            while (elapsed < 30 && !(spider.CarriedNote is { Opacity: >= 1.0 } && spider.Position.Y >= holdY - 1))
            {
                spider.Update(world, FrameSeconds);
                elapsed += FrameSeconds;
            }
            return elapsed;
        }

        // Bir noktayı kartın kendi eksenine çevirir: X kartın üst kenarı boyunca, Y kartın aşağısına doğru
        private static Vector InCardFrame(CarriedNote note, Point point)
        {
            Vector v = point - note.TopCenter;
            double cos = Math.Cos(-note.Tilt), sin = Math.Sin(-note.Tilt);
            return new Vector(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }

        private static double RunUntilDeliveryEnds(SpiderEntity spider, WorldContext world, double maxSeconds = 120)
        {
            double elapsed = 0;
            while (elapsed < maxSeconds && spider.CurrentState is DeliveringNoteState)
            {
                spider.Update(world, FrameSeconds);
                elapsed += FrameSeconds;
            }
            return elapsed;
        }

        [Fact]
        public void WhileClimbingToTheCeiling_NoNoteIsShownYet()
        {
            var (spider, world) = StartDelivery();

            spider.Update(world, FrameSeconds);

            Assert.Null(spider.CarriedNote);
        }

        [Fact]
        public void WhileHanging_TheNoteIsBelowTheSpidersHead()
        {
            var (spider, world) = StartDelivery("Annemi ara");
            RunUntilHolding(spider, world);

            CarriedNote note = Assert.IsType<CarriedNote>(spider.CarriedNote);
            Assert.Equal("Annemi ara", note.Text);
            Assert.True(note.TopCenter.Y > spider.Position.Y, "Not, baş aşağı sarkan örümceğin altında olmalı");
            Assert.Equal(SpiderTuning.NoteHoldDistance, (note.TopCenter - spider.Position).Length, precision: 3);
        }

        [Fact]
        public void WhileHanging_BothFrontLegsGripTheTopEdgeOfTheNote()
        {
            var (spider, world) = StartDelivery();
            RunUntilHolding(spider, world);
            CarriedNote note = spider.CarriedNote!;

            var frontLegs = spider.Legs.Where(leg => leg.PairIndex == 0).ToArray();
            Assert.Equal(2, frontLegs.Length);
            foreach (var leg in frontLegs)
            {
                Point foot = Assert.NotNull(leg.GestureFoot);
                Assert.Equal(foot, leg.CurrentFoot);

                // Kart eğik olabilir; ayak, kartın kendi ekseninde üst kenarın üzerinde olmalı
                Vector onCard = InCardFrame(note, foot);
                Assert.Equal(0, onCard.Y, precision: 3);
                Assert.Equal(SpiderTuning.NoteGripHalfSpan, Math.Abs(onCard.X), precision: 3);

                // Bacak koparcasına uzamadan karta yetişebilmeli
                Point hip = spider.LocalToWorld(leg.LocalAttach);
                Assert.True((foot - hip).Length <= leg.MaxReach, "Ön bacak kartın kenarına yetişemiyor");
            }

            // Biri sağ, biri sol köşeyi tutar
            Assert.NotEqual(Math.Sign(InCardFrame(note, frontLegs[0].CurrentFoot).X), Math.Sign(InCardFrame(note, frontLegs[1].CurrentFoot).X));
        }

        [Fact]
        public void WhileHanging_TheNoteOnlyTiltsALittle()
        {
            var (spider, world) = StartDelivery();
            RunUntilHolding(spider, world);

            double maxTilt = 0;
            for (double t = 0; t < SpiderTuning.NoteHangSeconds - 1; t += FrameSeconds)
            {
                spider.Update(world, FrameSeconds);
                maxTilt = Math.Max(maxTilt, Math.Abs(spider.CarriedNote!.Tilt));
            }

            Assert.True(maxTilt < 15 * Math.PI / 180, $"Kart {maxTilt * 180 / Math.PI:0.0} derece eğildi");
        }

        [Fact]
        public void OtherLegsAreNotHoldingTheNote()
        {
            var (spider, world) = StartDelivery();
            RunUntilHolding(spider, world);

            Assert.All(spider.Legs.Where(leg => leg.PairIndex != 0), leg => Assert.Null(leg.GestureFoot));
        }

        [Fact]
        public void WhenFinished_ReleasesTheNoteAndItsLegs_AndGoesWandering()
        {
            var (spider, world) = StartDelivery();

            RunUntilDeliveryEnds(spider, world);

            Assert.IsType<WanderingState>(spider.CurrentState);
            Assert.Null(spider.CarriedNote);
            Assert.Null(spider.ActiveSilkLine);
            Assert.All(spider.Legs, leg => Assert.Null(leg.GestureFoot));
        }

        [Fact]
        public void HoldsTheNoteForTheWholeHangTime()
        {
            var (spider, world) = StartDelivery();
            RunUntilHolding(spider, world);

            double remaining = RunUntilDeliveryEnds(spider, world);

            Assert.True(remaining >= SpiderTuning.NoteHangSeconds, $"Örümcek notu {remaining:0.0} sn sonra topladı");
        }

        [Fact]
        public void FastCursorPush_SendsTheSpiderBackUpEarly()
        {
            var (spider, world) = StartDelivery();
            RunUntilHolding(spider, world);

            world.CursorPosition = spider.Position;
            world.CursorSpeed = SpiderTuning.RappelCursorPushSpeed * 2;
            double remaining = RunUntilDeliveryEnds(spider, world);

            Assert.True(remaining < SpiderTuning.NoteHangSeconds / 2, $"Fare itince örümcek {remaining:0.0} sn sonra gitti");
        }

        [Fact]
        public void RequestedNote_WhileWandering_StartsRightAway()
        {
            var world = new WorldContext();
            var spider = new SpiderEntity(new Point(900, 700), new SpiderGaitController());

            spider.Messages.RequestNote("Annemi ara");
            spider.Update(world, FrameSeconds);

            var delivering = Assert.IsType<DeliveringNoteState>(spider.CurrentState);
            Assert.Equal("Annemi ara", delivering.Text);
            Assert.Equal(0, spider.Messages.PendingNoteCount);
        }

        [Fact]
        public void RequestedNote_WhileSpinningAWeb_WaitsUntilTheWebIsDone()
        {
            var world = new WorldContext();
            var spider = new SpiderEntity(new Point(900, 700), new SpiderGaitController());
            spider.TransitionTo(SpinningWebState.CreateForBestSite(world), world);

            spider.Messages.RequestNote("Annemi ara");
            spider.Update(world, FrameSeconds);

            Assert.IsType<SpinningWebState>(spider.CurrentState);
            Assert.Equal(1, spider.Messages.PendingNoteCount);

            // Ağ bitince örümcek notu göstermeye gider
            for (double t = 0; t < 120 && spider.CurrentState is SpinningWebState; t += FrameSeconds)
            {
                spider.Update(world, FrameSeconds);
            }
            Assert.True(world.Webs[0].IsCompleted, "Ağ yarıda bırakılmamalı");

            // Ağ bitince örümcek ağında beklemeye geçer, bekleyen not bir sonraki karede başlar
            spider.Update(world, FrameSeconds);
            Assert.IsType<DeliveringNoteState>(spider.CurrentState);
        }

        [Fact]
        public void TwoNotes_AreShownOneAfterAnother()
        {
            var world = new WorldContext();
            var spider = new SpiderEntity(new Point(900, 700), new SpiderGaitController());

            spider.Messages.RequestNote("Annemi ara");
            spider.Messages.RequestNote("Su iç");
            spider.Update(world, FrameSeconds);
            Assert.Equal("Annemi ara", Assert.IsType<DeliveringNoteState>(spider.CurrentState).Text);

            RunUntilDeliveryEnds(spider, world);
            spider.Update(world, FrameSeconds);

            Assert.Equal("Su iç", Assert.IsType<DeliveringNoteState>(spider.CurrentState).Text);
            Assert.Equal(0, spider.Messages.PendingNoteCount);
        }
    }
}
