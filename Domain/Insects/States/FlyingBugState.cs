using System;
using System.Windows;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Insects.States
{
    /// <summary>
    /// Böceğin masaüstünde uçtuğu, zaman zaman dinlenmek için konduğu veya ağa yakalandığı durum (OCP & SRP).
    /// </summary>
    public class FlyingBugState : IBugBehaviorState
    {
        private double _wanderAngle;
        private double _perchDecisionTimer;
        private double _webImmunityTimer;

        public BugLifecycleStatus Status => BugLifecycleStatus.ActiveInWorld;
        public bool IsReadyToBeHunted => false;

        public FlyingBugState(double webImmunitySeconds = 0.0)
        {
            _webImmunityTimer = webImmunitySeconds;
        }

        public void Enter(BugEntity bug, WorldContext world)
        {
            _wanderAngle = bug.Angle;
            _perchDecisionTimer = 4.0 + world.Random.NextDouble() * 5.0;
            bug.TrappedWeb = null;
        }

        public void Update(BugEntity bug, WorldContext world, double dt)
        {
            if (_webImmunityTimer > 0)
            {
                _webImmunityTimer -= dt;
            }

            bug.AdvanceWaves(dt * 65.0, dt * 4.2);
            _wanderAngle += (world.Random.NextDouble() - 0.5) * 8.5 * dt;

            const double margin = 75.0;
            Vector steering = new Vector(0, 0);
            if (bug.Position.X < margin) steering.X += 190;
            if (bug.Position.X > world.ScreenWidth - margin) steering.X -= 190;
            if (bug.Position.Y < margin) steering.Y += 190;
            if (bug.Position.Y > world.ScreenHeight - margin) steering.Y -= 190;

            if (world.Webs.Count > 0 && world.Random.NextDouble() < 0.04)
            {
                var targetWeb = world.Webs[world.Random.Next(world.Webs.Count)];
                if (targetWeb.IsCompleted)
                {
                    Vector toWeb = targetWeb.HubPosition - bug.Position;
                    if (toWeb.Length > 10 && toWeb.Length < 480)
                    {
                        toWeb.Normalize();
                        steering += toWeb * 110;
                    }
                }
            }

            Vector wanderForce = new Vector(Math.Cos(_wanderAngle), Math.Sin(_wanderAngle)) * 90.0;
            Vector nextVelocity = bug.Velocity + (wanderForce + steering) * dt;

            double speed = nextVelocity.Length;
            if (speed > bug.Species.MaxSpeed)
            {
                nextVelocity = (nextVelocity / speed) * bug.Species.MaxSpeed;
            }
            else if (speed < bug.Species.MinSpeed && speed > 0.001)
            {
                nextVelocity = (nextVelocity / speed) * bug.Species.MinSpeed;
            }

            bug.SetMotion(bug.Position + nextVelocity * dt, nextVelocity, Math.Atan2(nextVelocity.Y, nextVelocity.X));

            // 1. Ağa yakalanma kontrolü (Yeni kurtulmuş böcek kısa süre aynı ağa yapışmaz)
            if (_webImmunityTimer <= 0)
            {
                foreach (var web in world.Webs)
                {
                    if (web.CanCatchPoint(bug.Position))
                    {
                        double huntDelay = 3.5 + world.Random.NextDouble() * 3.0;
                        bug.TransitionTo(new TrappedBugState(web, huntDelay), world);
                        return;
                    }
                }
            }

            // 2. Masaüstüne konup dinlenme kararı
            if (bug.Species.CanPerchOnDesktop)
            {
                _perchDecisionTimer -= dt;
                if (_perchDecisionTimer <= 0)
                {
                    _perchDecisionTimer = 5.0 + world.Random.NextDouble() * 6.0;
                    bool insideScreen = bug.Position.X > 120 && bug.Position.X < world.ScreenWidth - 120 &&
                                        bug.Position.Y > 120 && bug.Position.Y < world.ScreenHeight - 140;

                    if (insideScreen && world.Random.NextDouble() < 0.35)
                    {
                        bug.TransitionTo(new PerchingBugState(), world);
                    }
                }
            }
        }
    }
}
