using System;
using System.Windows;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Fare imleci örümceğe hızla yaklaştığında ürküp güvenli bir noktaya kaçtığı durum.
    /// </summary>
    public class StartledByMouseState : ISpiderBehaviorState
    {
        private readonly ISpiderBehaviorState _returnState;
        private Point _escapeTarget;
        private double _alertPauseTimer = SpiderTuning.StartleAlertPauseSeconds;
        private bool _reachedSafety;

        public string DisplayName => "Fareden Ürktü!";
        public bool CanBeStartledByCursor => false;
        public bool CanHuntPrey => false;
        public bool CanEatFromPantry => false;

        public StartledByMouseState(ISpiderBehaviorState returnState)
        {
            _returnState = returnState;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = spider.Position;

            Vector awayFromMouse = spider.Position - world.CursorPosition;
            if (awayFromMouse.Length < 1.0)
            {
                awayFromMouse = new Vector(1, -1);
            }
            awayFromMouse.Normalize();

            double fleeDistance = SpiderTuning.StartleFleeMinDistance + world.Random.NextDouble() * SpiderTuning.StartleFleeDistanceRange;
            Point rawTarget = spider.Position + awayFromMouse * fleeDistance;

            // Ekran sınırları içinde tut
            _escapeTarget = new Point(
                Math.Clamp(rawTarget.X, 90, world.ScreenWidth - 90),
                Math.Clamp(rawTarget.Y, 90, world.ScreenHeight - 110)
            );
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            if (!_reachedSafety)
            {
                if (spider.MoveTowards(_escapeTarget, SpiderTuning.StartleFleeSpeed, dt, stopDistance: 6.0))
                {
                    _reachedSafety = true;
                }
            }
            else
            {
                // Güvenli noktaya varınca kısa süre fareye doğru dönüp tetikte bekler
                Vector toMouse = world.CursorPosition - spider.Position;
                if (toMouse.Length > 5.0)
                {
                    double watchAngle = Math.Atan2(toMouse.Y, toMouse.X);
                    spider.RotateTowards(watchAngle, 10.0, dt);
                }

                _alertPauseTimer -= dt;
                if (_alertPauseTimer <= 0)
                {
                    spider.TransitionTo(_returnState, world);
                }
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            spider.DraglineAnchor = null;
        }
    }
}
