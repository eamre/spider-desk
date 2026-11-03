using System;
using System.Windows;
using SpiderDesk.Core;

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
        private double _targetDropY;
        private double _currentDropLength;
        private double _swayTime;
        private double _swayAmplitude = 10.0;
        private double _hangTimer;

        public string DisplayName => "İple Sarkıyor";

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
                    if (spider.MoveTowards(new Point(_ceilingAnchor.X, 24), 165.0, dt))
                    {
                        _currentDropLength = 24;
                        _phase = Phase.Descending;
                    }
                    break;

                case Phase.Descending:
                    _currentDropLength += 115.0 * dt;
                    UpdateHangingPosition(spider, dt);

                    // İnerken fare yaklaşırsa hemen yukarı tırmanmaya geç
                    if (IsCursorPushingSpider(spider, world))
                    {
                        _swayAmplitude = 28.0;
                        _phase = Phase.Ascending;
                        break;
                    }

                    if (_currentDropLength >= _targetDropY)
                    {
                        _phase = Phase.HangingAndSwaying;
                    }
                    break;

                case Phase.HangingAndSwaying:
                    _hangTimer -= dt;
                    _swayAmplitude = Math.Max(6.0, _swayAmplitude * Math.Pow(0.65, dt));
                    UpdateHangingPosition(spider, dt);

                    // Fare yaklaşırsa ipi sallandır ve yukarı kaç!
                    if (IsCursorPushingSpider(spider, world))
                    {
                        _swayAmplitude = 32.0;
                        _phase = Phase.Ascending;
                        break;
                    }

                    if (_hangTimer <= 0)
                    {
                        _phase = Phase.Ascending;
                    }
                    break;

                case Phase.Ascending:
                    _currentDropLength -= 145.0 * dt;
                    UpdateHangingPosition(spider, dt, facingUp: true);

                    if (_currentDropLength <= 28)
                    {
                        spider.ActiveSilkLine = null;
                        if (spider.ActiveWeb != null)
                        {
                            spider.TransitionTo(new WaitingOnWebState(spider.ActiveWeb), world);
                        }
                        else
                        {
                            spider.TransitionTo(new WanderingState(), world);
                        }
                    }
                    break;
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveSilkLine = null;
        }

        private void UpdateHangingPosition(SpiderEntity spider, double dt, bool facingUp = false)
        {
            _swayTime += dt * 2.8;
            double offsetX = Math.Sin(_swayTime) * _swayAmplitude * (_currentDropLength / 220.0);

            Point targetPos = new Point(_ceilingAnchor.X + offsetX, _currentDropLength);
            spider.MoveTowards(targetPos, 350.0, dt, stopDistance: 0.5);

            double desiredAngle = facingUp ? -Math.PI / 2.0 : Math.PI / 2.0;
            spider.RotateTowards(desiredAngle, 10.0, dt);

            spider.ActiveSilkLine = (_ceilingAnchor, spider.GetSpinneretPosition());
        }

        private static bool IsCursorPushingSpider(SpiderEntity spider, WorldContext world)
        {
            double distSq = (world.CursorPosition - spider.Position).LengthSquared;
            return distSq < 72.0 * 72.0 && world.CursorSpeed > 80.0;
        }
    }
}
