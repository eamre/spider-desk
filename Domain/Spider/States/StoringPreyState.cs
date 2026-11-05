using System.Windows;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümcek tokken yakaladığı ve ipekle sardığı avı hemen yemeyip ağın kenarındaki
    /// erzak deposuna (kiler) taşıyıp astığı durum (OCP & SRP).
    /// </summary>
    public class StoringPreyState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private readonly double _cocoonSize;
        private readonly ISpiderBehaviorState _returnState;
        private Point _storageTarget;

        public string DisplayName => "Avı Kilere Taşıyor";
        public bool CanHuntPrey => false;
        public bool CanEatFromPantry => false;

        public StoringPreyState(SpiderWebEntity web, double cocoonSize, ISpiderBehaviorState returnState)
        {
            _web = web;
            _cocoonSize = cocoonSize;
            _returnState = returnState;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveWeb = _web;
            spider.CarriedCocoonSize = _cocoonSize;
            _storageTarget = _web.Pantry.PickStoragePoint(
                _web.Center,
                _web.Radius,
                _web.StartAngle,
                _web.EndAngle,
                world.Random
            );
            spider.DraglineAnchor = spider.Position;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            if (spider.MoveTowards(_storageTarget, SpiderTuning.StorePreyMoveSpeed, dt, stopDistance: 6.0))
            {
                // 1. Kozayı ağın kilerine (erzak deposuna) ekle
                _web.Pantry.StoreCocoon(_storageTarget, _cocoonSize, spider.Angle);
                spider.CarriedCocoonSize = null;

                // Ağda şu an çırpınan canlı/yakalanmış başka böcek var mı kontrol et:
                bool hasLivePreyOnWeb = false;
                for (int i = 0; i < world.Bugs.Count; i++)
                {
                    if (world.Bugs[i].CurrentState.Status == BugLifecycleStatus.TrappedInWeb && world.Bugs[i].TrappedWeb == _web)
                    {
                        hasLivePreyOnWeb = true;
                        break;
                    }
                }

                // 2. Eğer ağda başka canlı böcek YOKSA ve örümcek açsa, bulunduğu ağdaki eski erzakla beslensin:
                // Ağda canlı böcek varken asla yemeye dalmaz; canlı avı kaçırmamak birinci önceliktir.
                if (!hasLivePreyOnWeb && spider.Metabolism.IsHungry)
                {
                    var foodWeb = spider.FindWebWithStoredFood(world);
                    if (foodWeb == _web && foodWeb.Pantry.PeekNextMeal() is { } nextMeal)
                    {
                        spider.TransitionTo(new EatingFromPantryState(foodWeb, nextMeal), world);
                        return;
                    }
                }

                if (_web.HasDamage && _returnState is not SpinningWebState)
                {
                    spider.TransitionTo(new RepairingWebState(_web), world);
                }
                else
                {
                    spider.TransitionTo(_returnState, world);
                }
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            spider.CarriedCocoonSize = null;
            spider.DraglineAnchor = null;
        }
    }
}
