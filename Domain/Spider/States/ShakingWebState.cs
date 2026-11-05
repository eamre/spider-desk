using System;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin sadece ağın nöbet merkezine ulaştıktan sonra ağı güçlüce silkeleyerek
    /// su damlalarını düşürdüğü durum.
    /// </summary>
    public class ShakingWebState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private bool _dropletsReleased;
        private double _shakeTimer = 1.25;
        private double _shakePhase;

        public string DisplayName => "Ağdaki Suları Silkeliyor";
        public bool CanHuntPrey => false;
        public bool CanEatFromPantry => false;
        public bool CanBeStartledByCursor => false;

        public ShakingWebState(SpiderWebEntity web)
        {
            _web = web;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveWeb = _web;
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            if ((spider.Position - _web.HubPosition).Length > 3.0)
            {
                spider.MoveTowards(_web.HubPosition, SpiderTuning.ApproachWebSiteSpeed, dt, stopDistance: 2.0);
                return;
            }

            _shakePhase += dt * 45.0;
            _shakeTimer -= dt;

            spider.RotateBy(Math.Sin(_shakePhase) * 9.0 * dt);
            _web.TriggerVibration(_web.HubPosition, 9.0);

            if (!_dropletsReleased && _shakeTimer <= 0.75)
            {
                _dropletsReleased = true;
                _web.Droplets.ShakeOffAllDroplets(_web.CompletedSegments, _web.Center, world.Random);
            }

            if (_shakeTimer <= 0)
            {
                spider.TransitionTo(new WaitingOnWebState(_web), world);
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
        }
    }
}
