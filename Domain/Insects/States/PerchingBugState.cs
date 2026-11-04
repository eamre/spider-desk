using System;
using System.Windows;

namespace SpiderDesk.Domain.Insects.States
{
    /// <summary>
    /// Böceğin masaüstünde bir noktaya konup kanatlarını kapatarak dinlendiği durum.
    /// Fare imleci yaklaşırsa ürküp tekrar havalanır (OCP & SRP).
    /// </summary>
    public class PerchingBugState : IBugBehaviorState
    {
        private double _restTimer;

        public BugLifecycleStatus Status => BugLifecycleStatus.ActiveInWorld;
        public bool IsReadyToBeHunted => false;

        public void Enter(BugEntity bug, WorldContext world)
        {
            _restTimer = 4.0 + world.Random.NextDouble() * 4.5;
            // Kanatları kapat (WingPhase = 0) ve hızı sıfırla
            bug.SetMotion(bug.Position, new Vector(0, 0), bug.Angle);
            bug.ResetWings();
        }

        public void Update(BugEntity bug, WorldContext world, double dt)
        {
            // Ateş böceği ise konarken de ışığı nefes alır gibi yanıp söner
            bug.AdvanceWaves(0, dt * 2.5);

            // Fare imleci konmuş böceğe yaklaşırsa hemen ürküp havalan!
            if ((world.CursorPosition - bug.Position).LengthSquared < 65.0 * 65.0 && world.CursorSpeed > 40.0)
            {
                TakeOff(bug, world);
                return;
            }

            _restTimer -= dt;
            if (_restTimer <= 0)
            {
                TakeOff(bug, world);
            }
        }

        private static void TakeOff(BugEntity bug, WorldContext world)
        {
            double angle = bug.Angle + (world.Random.NextDouble() - 0.5) * 1.2;
            Vector launchVel = new Vector(Math.Cos(angle), Math.Sin(angle)) * bug.Species.MinSpeed;
            bug.SetMotion(bug.Position, launchVel, angle);
            bug.TransitionTo(new FlyingBugState(), world);
        }
    }
}
