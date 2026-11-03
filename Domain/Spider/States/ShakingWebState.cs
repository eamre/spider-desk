using System;
using SpiderDesk.Core;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin sadece ağın tam merkezine ulaştıktan sonra ağı güçlüce silkeleyerek
    /// su damlalarını düşürdüğü durum (OCP & SRP).
    /// </summary>
    public class ShakingWebState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private bool _dropletsReleased;
        private double _shakeTimer = 1.25;
        private double _shakePhase;

        public string DisplayName => "Ağdaki Suları Silkeliyor";

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
            // Örümcek ağın tam ortasında değilse önce tam merkeze yürü (merkeze varmadan asla silkeleme yapmaz)
            if ((spider.Position - _web.Center).Length > 3.0)
            {
                spider.MoveTowards(_web.Center, 175.0, dt, stopDistance: 2.0);
                return;
            }

            _shakePhase += dt * 45.0;
            _shakeTimer -= dt;

            // Tam merkezdeyken gövdeyi hızla sağa sola salla ve ağa merkezden titreşim ver
            spider.RotateBy(Math.Sin(_shakePhase) * 9.0 * dt);
            _web.TriggerVibration(_web.Center, 9.0);

            // Silkelemenin ortasında tüm su damlaları telden kopup aşağı düşmeye başlar
            if (!_dropletsReleased && _shakeTimer <= 0.75)
            {
                _dropletsReleased = true;
                _web.Droplets.ShakeOffAllDroplets(_web.CompletedSegments, world.Random);
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
