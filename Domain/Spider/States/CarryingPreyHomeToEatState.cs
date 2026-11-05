using System;
using System.Windows;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümcek açken masaüstünde yakaladığı avı ağzında güvenli yuvasına (ağın nöbet merkezine)
    /// taşıyıp orada doğrudan afiyetle tükettiği durum (Koy-al paradoksunu önler).
    /// </summary>
    public class CarryingPreyHomeToEatState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private readonly double _cocoonSize;
        private readonly ISpiderBehaviorState _nextState;
        private bool _arrivedAtHub;
        private double _eatProgress;
        private double _chewPhase;

        public string DisplayName => _arrivedAtHub ? "Avını Yiyor" : "Avı Yuvasına Taşıyor";
        public bool CanHuntPrey => false;
        public bool CanEatFromPantry => false;

        public CarryingPreyHomeToEatState(SpiderWebEntity web, double cocoonSize, ISpiderBehaviorState nextState)
        {
            _web = web;
            _cocoonSize = cocoonSize;
            _nextState = nextState;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveWeb = _web;
            spider.CarriedCocoonSize = _cocoonSize;
            spider.DraglineAnchor = spider.Position;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            if (!_arrivedAtHub)
            {
                if (spider.MoveTowards(_web.HubPosition, SpiderTuning.StorePreyMoveSpeed, dt, stopDistance: 6.0))
                {
                    _arrivedAtHub = true;
                    spider.DraglineAnchor = null;
                }
                return;
            }

            // Yuvasına vardı: Avını kıskaçlarında çiğneyerek tüketir
            Vector toHub = _web.HubPosition - spider.Position;
            if (toHub.Length > 1.0)
            {
                double desiredAngle = Math.Atan2(toHub.Y, toHub.X);
                spider.RotateTowards(desiredAngle, 10.0, dt);
            }

            _chewPhase += dt * 14.0;
            spider.RotateBy(Math.Sin(_chewPhase) * 0.45 * dt);

            _eatProgress += dt * SpiderTuning.CocoonConsumeRate;
            // Yedikçe ağzındaki koza görsel olarak küçülür
            spider.CarriedCocoonSize = Math.Max(1.0, _cocoonSize * (1.0 - _eatProgress * 0.85));

            if (_eatProgress >= 1.0)
            {
                spider.CarriedCocoonSize = null;
                spider.Metabolism.Feed(SpiderTuning.PreyNutrition);

                if (_web.HasDamage && _nextState is not SpinningWebState)
                {
                    spider.TransitionTo(new RepairingWebState(_web), world);
                }
                else
                {
                    spider.TransitionTo(_nextState, world);
                }
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            if (_eatProgress >= 1.0)
            {
                spider.CarriedCocoonSize = null;
            }
            spider.DraglineAnchor = null;
        }
    }
}
