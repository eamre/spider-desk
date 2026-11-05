using System;
using System.Linq;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Insects.States;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin yakalanan veya yolda denk geldiği böceğe koştuğu, onu ipekle sardığı ve ağa taşıdığı durum.
    /// </summary>
    public class HuntingState : ISpiderBehaviorState
    {
        private readonly BugEntity _targetBug;
        private readonly ISpiderBehaviorState _previousState;
        private bool _isWrapping;

        public string DisplayName => _isWrapping ? "Avı İpekle Sarıyor" : "Avlanıyor!";
        public bool CanBeStartledByCursor => false;
        public bool CanHuntPrey => false;
        public bool CanEatFromPantry => false;

        public HuntingState(BugEntity targetBug, ISpiderBehaviorState previousState)
        {
            _targetBug = targetBug;
            _previousState = previousState;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            if (_targetBug.TrappedWeb != null)
            {
                spider.ActiveWeb = _targetBug.TrappedWeb;
            }
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = spider.Position;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            const double maxWildChaseDistSq = SpiderTuning.WildBugMaxChaseDistance * SpiderTuning.WildBugMaxChaseDistance;

            bool isTrapped = _targetBug.CurrentState.Status == BugLifecycleStatus.TrappedInWeb;
            bool isReachableWildBug = _targetBug.CurrentState.Status == BugLifecycleStatus.ActiveInWorld &&
                                      (_targetBug.Position - spider.Position).LengthSquared <= maxWildChaseDistSq;

            // Eğer böcek yenildiyse, güçlü bir böcek olup ağdan kurtulduysa veya masaüstünde biz yetişemeden uzağa uçtuysa:
            if (_targetBug.IsEaten || (!_isWrapping && !isTrapped && !isReachableWildBug))
            {
                FinishHunt(spider, world);
                return;
            }

            if (!_isWrapping)
            {
                double chaseSpeed = isTrapped ? SpiderTuning.ChaseTrappedBugSpeed : SpiderTuning.ChaseWildBugSpeed;
                double catchDistance = isTrapped ? SpiderTuning.TrappedCatchStopDistance : SpiderTuning.WildCatchStopDistance;
                if (spider.MoveTowards(_targetBug.Position, chaseSpeed, dt, stopDistance: catchDistance))
                {
                    _isWrapping = true;
                    _targetBug.TransitionTo(new BeingWrappedBugState(), world);
                }
            }
            else
            {
                spider.RotateBy(Math.Sin(_targetBug.WrapProgress * Math.PI * 6) * 4.5 * dt);
                _targetBug.WrapProgress += dt * SpiderTuning.WrapProgressRate;
                _targetBug.TrappedWeb?.TriggerVibration(_targetBug.Position, 3.0);

                if (_targetBug.WrapProgress >= 1.0)
                {
                    spider.RecordPreyCaught();

                    bool caughtOnWeb = _targetBug.TrappedWeb != null;
                    SpiderWebEntity? targetWeb = _targetBug.TrappedWeb
                        ?? (spider.ActiveWeb is { IsCompleted: true } ? spider.ActiveWeb : null)
                        ?? world.Webs
                            .Where(w => w.IsCompleted)
                            .OrderBy(w => (w.HubPosition - spider.Position).LengthSquared)
                            .FirstOrDefault()
                        ?? spider.ActiveWeb;

                    bool anyPantryHasFood = spider.FindWebWithStoredFood(world) != null;

                    // Ağa takılan böcekte: Örümcek açsa ve hiçbir ağın erzağında böcek yoksa olduğu yerde (ağda) yer.
                    // Yürürken masaüstünde denk geldiği böcekte ise: Kozayı gideceği ağa taşır ve açsa ağa varınca yer!
                    bool eatOnTheSpot = caughtOnWeb && spider.Metabolism.IsHungry && !anyPantryHasFood;

                    if (eatOnTheSpot || targetWeb == null)
                    {
                        spider.Metabolism.Feed(SpiderTuning.PreyNutrition);
                        FinishHunt(spider, world);
                    }
                    else if (!targetWeb.IsCompleted)
                    {
                        // Gideceği ağ henüz örülmemişse kozayı ağzında taşıyarak ağ örmeye devam eder; ağ bitince kilere asar / yer.
                        spider.CarriedCocoonSize = _targetBug.Size;
                        FinishHunt(spider, world);
                    }
                    else
                    {
                        // Av paketlenip bu ağa asıldıktan sonra eski ağa geri dönülmez; örümcek artık bu ağdadır.
                        ISpiderBehaviorState nextStateAfterStore = _previousState is SpinningWebState
                            ? _previousState
                            : new WaitingOnWebState(targetWeb);

                        if (spider.Metabolism.IsHungry && !anyPantryHasFood)
                        {
                            spider.TransitionTo(new CarryingPreyHomeToEatState(targetWeb, _targetBug.Size, nextStateAfterStore), world);
                        }
                        else
                        {
                            spider.TransitionTo(new StoringPreyState(targetWeb, _targetBug.Size, nextStateAfterStore), world);
                        }
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
            if (_previousState is SpinningWebState)
            {
                spider.TransitionTo(_previousState, world);
            }
            else if (trappedWeb is { HasDamage: true })
            {
                spider.ActiveWeb = trappedWeb;
                spider.TransitionTo(new RepairingWebState(trappedWeb), world);
            }
            else if (trappedWeb != null)
            {
                spider.ActiveWeb = trappedWeb;
                spider.TransitionTo(new WaitingOnWebState(trappedWeb), world);
            }
            else
            {
                spider.TransitionTo(_previousState, world);
            }
        }
    }
}
