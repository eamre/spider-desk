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
        private double _anchorAngle;
        private double _trappedDuration;
        private double _struggleTimer;
        private double _escapeTimer = 2.5;

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
            _anchorAngle = bug.Angle;
            _trappedDuration = 0;
            _escapeTimer = 2.5 + world.Random.NextDouble() * 1.0;

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
            // 1. Ağ artık dünyada mevcut değilse (Ağları Temizle dendi veya ağ silindi) hemen serbest kalıp uçar!
            if (_web != null && !world.Webs.Contains(_web))
            {
                FreeBug(bug, world);
                return;
            }

            _trappedDuration += dt;
            bug.AdvanceWaves(dt * 95.0, dt * 6.0);

            _struggleTimer -= dt;
            if (_struggleTimer <= 0)
            {
                _struggleTimer = 0.45 + world.Random.NextDouble() * 0.70;
                _web?.TriggerVibration(bug.Position, 5.5);
            }

            // 2. Sadece güçlü böcekler (Eşek/Yaban Arısı) periyodik olarak ağdan kurtulmayı dener!
            _escapeTimer -= dt;
            if (_escapeTimer <= 0)
            {
                _escapeTimer = 3.5 + world.Random.NextDouble() * 1.5;
                if (bug.Species.EscapeChance > 0 && world.Random.NextDouble() < bug.Species.EscapeChance)
                {
                    // Ağı yırtıp kurtuldu (sadece eşek arısı telleri koparabilir)
                    if (bug.Species.WebDamageOnImpact > 0)
                    {
                        _web?.DamageNear(_anchor, damageRadius: 32.0, maxBrokenSegments: 2);
                    }
                    FreeBug(bug, world);
                    return;
                }
            }

            double shakeX = Math.Sin(bug.WingPhase * 0.9) * 1.5;
            double shakeY = Math.Cos(bug.WingPhase * 1.1) * 1.5;
            Point shakenPos = new Point(_anchor.X + shakeX, _anchor.Y + shakeY);
            double shakenAngle = _anchorAngle + Math.Sin(bug.WingPhase) * 0.35;

            bug.SetMotion(shakenPos, new Vector(0, 0), shakenAngle);
        }

        private void FreeBug(BugEntity bug, WorldContext world)
        {
            bug.TrappedWeb = null;
            bug.WrapProgress = 0.0;
            double escapeAngle = world.Random.NextDouble() * Math.PI * 2;
            Vector escapeVel = new Vector(Math.Cos(escapeAngle), Math.Sin(escapeAngle)) * bug.Species.MinSpeed;
            bug.SetMotion(bug.Position, escapeVel, escapeAngle);
            bug.TransitionTo(new FlyingBugState(webImmunitySeconds: 4.0), world);
        }
    }
}
