using SpiderDesk.Core;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümcek acıktığında (%50 ve altı tokluk) önce bulunduğu ağdaki, orada yoksa diğer ağdaki
    /// saklanmış kozaların yanına gidip karnı doyana kadar (%75+) beslendiği durum (OCP & SRP).
    /// </summary>
    public class EatingFromPantryState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private readonly StoredCocoon _cocoon;
        private bool _reachedCocoon;

        public string DisplayName => "Kilerden Besleniyor";

        public EatingFromPantryState(SpiderWebEntity web, StoredCocoon cocoon)
        {
            _web = web;
            _cocoon = cocoon;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveWeb = _web;
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            if (!_reachedCocoon)
            {
                if (spider.MoveTowards(_cocoon.Position, 170.0, dt, stopDistance: 12.0))
                {
                    _reachedCocoon = true;
                }
                return;
            }

            // Kozayı tüket ve enerjini doldur (+%25 tokluk)
            _cocoon.ConsumeProgress += dt * 0.65;
            if (_cocoon.ConsumeProgress >= 1.0)
            {
                _web.Pantry.RemoveCocoon(_cocoon);
                spider.Metabolism.Feed(25.0);

                // Hâlâ tam doymadıysa (%75'in altındaysa) ve bu ağda veya başka bir ağda koza varsa yemeye devam et!
                if (!spider.Metabolism.IsFull)
                {
                    var nextFoodWeb = spider.FindWebWithStoredFood(world);
                    if (nextFoodWeb?.Pantry.PeekNextMeal() is { } nextCocoon)
                    {
                        spider.TransitionTo(new EatingFromPantryState(nextFoodWeb, nextCocoon), world);
                        return;
                    }
                }

                spider.TransitionTo(new WaitingOnWebState(_web), world);
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
        }
    }
}
