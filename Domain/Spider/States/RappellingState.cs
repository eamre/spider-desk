using System;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin ekranın üst kenarına çıkıp ipek bir iple aşağı sarktığı, sallandığı ve geri tırmandığı durum.
    /// Ne kadar aşağı ineceği ve ne kadar sallanacağı her seferinde rastgele seçilir (SpiderTuning'in "Normal sarkma" bölümü).
    /// </summary>
    public class RappellingState : ISpiderBehaviorState
    {
        private enum Phase
        {
            ClimbingToCeiling,
            Descending,
            HangingAndSwaying,
            Ascending
        }

        private Phase _phase = Phase.ClimbingToCeiling;
        private double _anchorX;
        private HangingSilkLine _line = null!;
        private double _targetDropY;
        private double _hangTimer;

        public string DisplayName => "İple Sarkıyor";
        public bool CanBeStartledByCursor => false;
        public bool CanHuntPrey => false;
        public bool CanEatFromPantry => false;

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            double shift = (world.Random.NextDouble() - 0.5) * SpiderTuning.RappelAnchorSpread;
            _anchorX = Math.Clamp(
                spider.Position.X + shift,
                SpiderTuning.RappelScreenEdgeMargin,
                world.ScreenWidth - SpiderTuning.RappelScreenEdgeMargin);
            _targetDropY = SpiderTuning.RappelMinDrop + world.Random.NextDouble() * (world.ScreenHeight * SpiderTuning.RappelExtraDropRatio);
            _hangTimer = SpiderTuning.RappelMinHangSeconds + world.Random.NextDouble() * SpiderTuning.RappelExtraHangSeconds;
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            switch (_phase)
            {
                case Phase.ClimbingToCeiling:
                    if (spider.MoveTowards(HangingSilkLine.ClimbTarget(_anchorX), SpiderTuning.RappelClimbSpeed, dt))
                    {
                        _line = new HangingSilkLine(_anchorX);
                        _phase = Phase.Descending;
                    }
                    break;

                case Phase.Descending:
                    _line.Length += SpiderTuning.RappelDescendSpeed * dt;
                    _line.MoveSpider(spider, dt);

                    // İnerken fare yaklaşırsa hemen yukarı tırmanmaya geç
                    if (HangingSilkLine.IsCursorPushing(spider, world))
                    {
                        _line.SwayAmplitude = SpiderTuning.RappelPushedSwayWhileDescending;
                        _phase = Phase.Ascending;
                        break;
                    }

                    if (_line.Length >= _targetDropY)
                    {
                        _phase = Phase.HangingAndSwaying;
                    }
                    break;

                case Phase.HangingAndSwaying:
                    _hangTimer -= dt;
                    _line.DampSway(dt);
                    _line.MoveSpider(spider, dt);

                    // Fare yaklaşırsa ipi sallandır ve yukarı kaç!
                    if (HangingSilkLine.IsCursorPushing(spider, world))
                    {
                        _line.SwayAmplitude = SpiderTuning.RappelPushedSwayWhileHanging;
                        _phase = Phase.Ascending;
                        break;
                    }

                    if (_hangTimer <= 0)
                    {
                        _phase = Phase.Ascending;
                    }
                    break;

                case Phase.Ascending:
                    _line.Length -= SpiderTuning.RappelAscendSpeed * dt;
                    _line.MoveSpider(spider, dt, facingUp: true);

                    if (_line.IsReeledIn)
                    {
                        spider.ActiveSilkLine = null;
                        ReturnHome(spider, world);
                    }
                    break;
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveSilkLine = null;
        }

        /// <summary>İp toplandıktan sonra örümcek ağı varsa ağına döner, yoksa gezinmeye başlar.</summary>
        internal static void ReturnHome(SpiderEntity spider, WorldContext world)
        {
            if (spider.ActiveWeb != null)
            {
                spider.TransitionTo(new WaitingOnWebState(spider.ActiveWeb), world);
            }
            else
            {
                spider.TransitionTo(new WanderingState(), world);
            }
        }
    }
}
