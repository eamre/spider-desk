using System;
using System.Windows;
using SpiderDesk.Core;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Fare ağın tellerini titrettiğinde örümceğin o noktaya gidip "av mı var?" diye kontrol ettiği durum.
    /// </summary>
    public class InvestigatingVibrationState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private Point _investigatePoint;
        private bool _arrivedAtPoint;
        private double _inspectTimer = 1.6;
        private double _lookPhase;

        public string DisplayName => "Titreşimi İnceliyor";
        public bool CanStartScheduledWeb => true;

        public InvestigatingVibrationState(SpiderWebEntity web, Point pluckPoint)
        {
            _web = web;
            _investigatePoint = pluckPoint;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            // Eğer incelerken kullanıcı başka bir teli titretirse hedefini oraya güncelle
            if (_web.RecentCursorPluckPoint is { } newPluck)
            {
                _web.RecentCursorPluckPoint = null;
                _investigatePoint = newPluck;
                _arrivedAtPoint = false;
                _inspectTimer = 1.5;
            }

            if (!_arrivedAtPoint)
            {
                if (spider.MoveTowards(_investigatePoint, 185.0, dt, stopDistance: 8.0))
                {
                    _arrivedAtPoint = true;
                }
            }
            else
            {
                // Noktaya varınca sağa sola bakınıp av arar
                _lookPhase += dt * 7.5;
                spider.RotateBy(Math.Sin(_lookPhase) * 2.2 * dt);

                _inspectTimer -= dt;
                if (_inspectTimer <= 0)
                {
                    spider.TransitionTo(new WaitingOnWebState(_web), world);
                }
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
        }
    }
}
