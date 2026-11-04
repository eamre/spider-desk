using System;
using System.Windows;
using SpiderDesk.Core;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin tamamladığı ağın nöbet merkezinde veya telleri üzerinde nöbet tuttuğu durum.
    /// </summary>
    public class WaitingOnWebState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private Point _patrolTarget;
        private double _patrolTimer;

        public string DisplayName => "Ağda Nöbette";
        public bool CanStartScheduledWeb => true;
        public bool IsAlwaysAnimating => false;

        public WaitingOnWebState(SpiderWebEntity web)
        {
            _web = web;
            _patrolTarget = web.HubPosition;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveWeb = _web;
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
            _patrolTarget = _web.HubPosition;
            _patrolTimer = 8.0 + world.Random.NextDouble() * 6.0;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            // 1. Ağda kopuk/hasarlı tel varsa öncelikle gidip onu onar!
            if (_web.HasDamage)
            {
                spider.TransitionTo(new RepairingWebState(_web), world);
                return;
            }

            // 2. Ağda çok fazla çiy/su damlası biriktiyse önce ağın nöbet merkezine yürü,
            // SADECE merkeze vardığında silkeleme durumuna geç!
            if (_web.Droplets.IsHeavyWithWater)
            {
                _patrolTarget = _web.HubPosition;
                if (spider.MoveTowards(_web.HubPosition, 155.0, dt, stopDistance: 2.5))
                {
                    spider.TransitionTo(new ShakingWebState(_web), world);
                }
                return;
            }

            // 3. Kullanıcı fareyle ağın tellerini titretti mi? Hemen kontrol etmeye git!
            if (_web.RecentCursorPluckPoint is { } pluckPoint)
            {
                _web.RecentCursorPluckPoint = null;
                spider.TransitionTo(new InvestigatingVibrationState(_web, pluckPoint), world);
                return;
            }

            spider.MoveTowards(_patrolTarget, 110.0, dt);
            _patrolTimer -= dt;

            if (_patrolTimer <= 0)
            {
                double roll = world.Random.NextDouble();
                if (roll < 0.16)
                {
                    spider.TransitionTo(new RappellingState(), world);
                }
                else if (roll < 0.36)
                {
                    spider.TransitionTo(new WanderingState(), world);
                }
                else
                {
                    double span = _web.EndAngle - _web.StartAngle;
                    double angle = _web.StartAngle + span * (0.15 + world.Random.NextDouble() * 0.70);
                    double dist = (0.18 + world.Random.NextDouble() * 0.28) * _web.Radius;

                    _patrolTarget = new Point(
                        _web.Center.X + Math.Cos(angle) * dist,
                        _web.Center.Y + Math.Sin(angle) * dist
                    );
                    _patrolTimer = 5.0 + world.Random.NextDouble() * 6.0;
                }
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
        }
    }
}
