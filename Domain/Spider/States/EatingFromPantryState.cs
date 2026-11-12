using System;
using System.Windows;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümcek acıktığında (%50 ve altı tokluk) önce bulunduğu ağdaki, orada yoksa diğer ağdaki
    /// saklanmış kozaların yanına gidip karnı doyana kadar (%75+) beslendiği durum.
    /// </summary>
    public class EatingFromPantryState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private readonly StoredCocoon _cocoon;
        private bool _reachedCocoon;
        private double _chewPhase;

        public string DisplayName => "Kilerden Besleniyor";
        public bool CanHuntPrey => false;
        public bool CanEatFromPantry => false;

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
                if (spider.MoveTowards(_cocoon.Position, SpiderTuning.EatMoveSpeed, dt, stopDistance: 12.0))
                {
                    _reachedCocoon = true;
                }
                return;
            }

            // Kozaya doğru dön ve çiğneme animasyonu yap
            Vector toCocoon = _cocoon.Position - spider.Position;
            if (toCocoon.Length > 1.0)
            {
                double desiredAngle = Math.Atan2(toCocoon.Y, toCocoon.X);
                spider.RotateTowards(desiredAngle, 10.0, dt);
            }

            _chewPhase += dt * 14.0;
            spider.RotateBy(Math.Sin(_chewPhase) * 0.45 * dt);

            // Kozayı tüket ve enerjini doldur
            _cocoon.ConsumeProgress += dt * SpiderTuning.CocoonConsumeRate;
            if (_cocoon.ConsumeProgress >= 1.0)
            {
                _web.Pantry.RemoveCocoon(_cocoon);
                spider.Metabolism.Feed(SpiderTuning.PreyNutrition);

                // Ağda şu an çırpınan canlı böcek var mı kontrol et:
                bool hasLivePreyOnWeb = false;
                for (int i = 0; i < world.Bugs.Count; i++)
                {
                    if (world.Bugs[i].CurrentState.Status == BugLifecycleStatus.TrappedInWeb && world.Bugs[i].TrappedWeb == _web)
                    {
                        hasLivePreyOnWeb = true;
                        break;
                    }
                }

                // Canlı böcek yoksa, hâlâ tam doymadıysa ve BU AĞIN kilerinde başka koza varsa yemeye devam et!
                // Başka ağa koşmasın, bu ağdaki işini bitirsin.
                if (!hasLivePreyOnWeb && !spider.Metabolism.IsFull && _web.Pantry.PeekNextMeal() is { } nextCocoon)
                {
                    spider.TransitionTo(new EatingFromPantryState(_web, nextCocoon), world);
                    return;
                }

                spider.TransitionTo(new WaitingOnWebState(_web), world);
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
        }
    }
}
