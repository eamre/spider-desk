using System;
using System.Windows;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Insects.States
{
    /// <summary>
    /// Böceğin ağa yapıştığı, çırpınarak ağı titrettiği ve güçlü bir türse
    /// belirli bir ihtimalle ağı yırtıp kurtulabildiği durum (OCP & SRP).
    /// </summary>
    public class TrappedBugState : IBugBehaviorState
    {
        private readonly SpiderWebEntity? _web;
        private readonly double _huntDelaySeconds;
        private Point _anchor;
        private double _trappedDuration;
        private double _struggleTimer;
        private bool _escapeAttempted;

        public BugLifecycleStatus Status => BugLifecycleStatus.TrappedInWeb;
        public bool IsReadyToBeHunted => _trappedDuration >= _huntDelaySeconds;

        public TrappedBugState(SpiderWebEntity? web, double huntDelaySeconds)
        {
            _web = web;
            _huntDelaySeconds = huntDelaySeconds;
        }

        public void Enter(BugEntity bug, WorldContext world)
        {
            bug.TrappedWeb = _web;
            _anchor = bug.Position;
            _trappedDuration = 0;

            if (_web != null)
            {
                _web.TriggerVibration(_anchor, 8.0);
                if (bug.Species.WebDamageOnImpact > 0)
                {
                    _web.DamageNear(_anchor, damageRadius: 38.0, maxBrokenSegments: bug.Species.WebDamageOnImpact);
                }
            }
        }

        public void Update(BugEntity bug, WorldContext world, double dt)
        {
            _trappedDuration += dt;
            bug.AdvanceWaves(dt * 95.0, dt * 6.0);

            _struggleTimer -= dt;
            if (_struggleTimer <= 0)
            {
                _struggleTimer = 0.45 + world.Random.NextDouble() * 0.70;
                _web?.TriggerVibration(bug.Position, 5.5);
            }

            // Örümcek gelmeden önce (~2.5 sn sonunda) güçlü böcekler ağdan kurtulmayı dener!
            if (!_escapeAttempted && _trappedDuration >= 2.5)
            {
                _escapeAttempted = true;
                if (world.Random.NextDouble() < bug.Species.EscapeChance)
                {
                    // Ağı yırtıp kurtuldu!
                    _web?.DamageNear(_anchor, damageRadius: 32.0, maxBrokenSegments: 2);
                    double escapeAngle = world.Random.NextDouble() * Math.PI * 2;
                    Vector escapeVel = new Vector(Math.Cos(escapeAngle), Math.Sin(escapeAngle)) * bug.Species.MaxSpeed;
                    bug.SetMotion(_anchor, escapeVel, escapeAngle);
                    bug.TransitionTo(new FlyingBugState(webImmunitySeconds: 3.5), world);
                    return;
                }
            }

            double shakeX = Math.Sin(bug.WingPhase * 0.9) * 1.5;
            double shakeY = Math.Cos(bug.WingPhase * 1.1) * 1.5;
            Point shakenPos = new Point(_anchor.X + shakeX, _anchor.Y + shakeY);
            double shakenAngle = bug.Angle + Math.Sin(bug.WingPhase) * 2.5 * dt;

            bug.SetMotion(shakenPos, new Vector(0, 0), shakenAngle);
        }
    }
}
