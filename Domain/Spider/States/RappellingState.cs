using System;
using System.Windows;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin ekranın üst kenarına çıkıp ipek bir iple aşağı sarktığı, sallandığı ve geri tırmandığı durum.
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
        private Point _ceilingAnchor;
        private HangingSilkLine _line = null!;
        private double _targetDropY;
        private double _hangTimer;

        public string DisplayName => "İple Sarkıyor";
        public bool CanBeStartledByCursor => false;
        public bool CanHuntPrey => false;
        public bool CanEatFromPantry => false;

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            double anchorX = Math.Clamp(spider.Position.X + (world.Random.NextDouble() - 0.5) * 160, 140, world.ScreenWidth - 140);
            _ceilingAnchor = new Point(anchorX, 2);
            _targetDropY = 180 + world.Random.NextDouble() * (world.ScreenHeight * 0.38);
            _hangTimer = 5.0 + world.Random.NextDouble() * 4.0;
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            switch (_phase)
            {
                case Phase.ClimbingToCeiling:
                    if (spider.MoveTowards(new Point(_ceilingAnchor.X, 24), SpiderTuning.RappelClimbSpeed, dt))
                    {
                        _line = new HangingSilkLine(_ceilingAnchor, length: 24, swayAmplitude: 10.0);
                        _phase = Phase.Descending;
                    }
                    break;

                case Phase.Descending:
                    _line.Length += SpiderTuning.RappelDescendSpeed * dt;
                    _line.MoveSpider(spider, dt);

                    // İnerken fare yaklaşırsa hemen yukarı tırmanmaya geç
                    if (HangingSilkLine.IsCursorPushing(spider, world))
                    {
                        _line.SwayAmplitude = 28.0;
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
                    _line.SwayAmplitude = Math.Max(6.0, _line.SwayAmplitude * Math.Pow(0.65, dt));
                    _line.MoveSpider(spider, dt);

                    // Fare yaklaşırsa ipi sallandır ve yukarı kaç!
                    if (HangingSilkLine.IsCursorPushing(spider, world))
                    {
                        _line.SwayAmplitude = 32.0;
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

                    if (_line.Length <= 28)
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
