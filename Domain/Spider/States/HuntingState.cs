using System;
using SpiderDesk.Core;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Insects.States;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin yakalanan böceğe koştuğu, onu ipekle sardığı ve ardından önceki görevine döndüğü durum.
    /// </summary>
    public class HuntingState : ISpiderBehaviorState
    {
        private readonly BugEntity _targetBug;
        private readonly ISpiderBehaviorState _previousState;
        private bool _isWrapping;

        public string DisplayName => _isWrapping ? "Avı İpekle Sarıyor" : "Avlanıyor!";

        public HuntingState(BugEntity targetBug, ISpiderBehaviorState previousState)
        {
            _targetBug = targetBug;
            _previousState = previousState;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = spider.Position;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            // Eğer böcek yenildiyse veya güçlü bir böcek olup ağdan kurtulduysa:
            if (_targetBug.IsEaten || (!_isWrapping && _targetBug.CurrentState.Status != BugLifecycleStatus.TrappedInWeb))
            {
                FinishHunt(spider, world);
                return;
            }

            if (!_isWrapping)
            {
                if (spider.MoveTowards(_targetBug.Position, 260.0, dt, stopDistance: 14.0))
                {
                    _isWrapping = true;
                    _targetBug.TransitionTo(new BeingWrappedBugState(), world);
                }
            }
            else
            {
                spider.RotateBy(Math.Sin(_targetBug.WrapProgress * Math.PI * 6) * 4.5 * dt);
                _targetBug.WrapProgress += dt * 0.85;
                _targetBug.TrappedWeb?.TriggerVibration(_targetBug.Position, 3.0);

                if (_targetBug.WrapProgress >= 1.0)
                {
                    spider.RecordPreyCaught();

                    var targetWeb = _targetBug.TrappedWeb ?? spider.ActiveWeb;
                    bool anyPantryHasFood = spider.FindWebWithStoredFood(world) != null;

                    // Örümcek AÇ ise ve hiçbir ağın erzağında böcek YOKSA: Erzağa taşımaya gerek yok, hemen olduğu yerde yesin.
                    // Ancak tok ise VEYA erzakta zaten böcek varsa: Yeni yakalananı önce erzağa eklesin, sonra (açsa) erzaktan yesin!
                    bool eatOnTheSpot = spider.Metabolism.IsHungry && !anyPantryHasFood;

                    if (eatOnTheSpot || targetWeb == null)
                    {
                        spider.Metabolism.Feed(25.0);
                        FinishHunt(spider, world);
                    }
                    else
                    {
                        spider.TransitionTo(new StoringPreyState(targetWeb, _targetBug.Size, _previousState), world);
                    }
                }
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            spider.DraglineAnchor = null;
        }

        private void FinishHunt(SpiderEntity spider, WorldContext world)
        {
            var trappedWeb = _targetBug.TrappedWeb;
            if (trappedWeb is { HasDamage: true } && _previousState is not SpinningWebState)
            {
                spider.TransitionTo(new RepairingWebState(trappedWeb), world);
            }
            else
            {
                spider.TransitionTo(_previousState, world);
            }
        }
    }
}
