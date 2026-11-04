using System.Windows;
using SpiderDesk.Core;
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
            if (spider.MoveTowards(_storageTarget, 155.0, dt, stopDistance: 6.0))
            {
                // 1. Kozayı ağın kilerine (erzak deposuna) ekle
                _web.Pantry.StoreCocoon(_storageTarget, _cocoonSize, spider.Angle);
                spider.CarriedCocoonSize = null;

                // 2. Eğer örümcek açsa, önce bulunduğu ağdaki (yoksa diğer ağdaki) eski erzaktan beslenmeye geçer!
                if (spider.Metabolism.IsHungry)
                {
                    var foodWeb = spider.FindWebWithStoredFood(world);
                    if (foodWeb?.Pantry.PeekNextMeal() is { } nextMeal)
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
