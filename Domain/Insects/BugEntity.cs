using System;
using System.Windows;
using SpiderDesk.Domain.Insects.Species;
using SpiderDesk.Domain.Insects.States;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Insects
{
    /// <summary>
    /// Tek bir böceğin fiziksel konumunu, türünü (IBugSpecies) ve aktif davranış durumunu (IBugBehaviorState) yönetir.
    /// </summary>
    public class BugEntity
    {
        /// <summary>Ağdan kurtulan böcek bu kadar saniye hiçbir ağa yakalanmaz; hemen aynı ağa geri yapışmasın.</summary>
        public const double WebImmunityAfterReleaseSeconds = 4.0;

        public IBugSpecies Species { get; }
        public IBugBehaviorState CurrentState { get; private set; }

        public Point Position { get; private set; }
        public Vector Velocity { get; private set; }
        public double Angle { get; private set; }
        public double Size { get; }

        public double WingPhase { get; private set; }
        public double GlowPhase { get; private set; }
        public double WrapProgress { get; set; }
        public SpiderWebEntity? TrappedWeb { get; set; }

        public bool IsEaten => CurrentState.Status == BugLifecycleStatus.Eaten;

        /// <summary>Böcek bir ağa yapışmış ya da örümcek onu kozaya sarıyorsa true döner.</summary>
        public bool IsCaughtInWeb => CurrentState.Status is BugLifecycleStatus.TrappedInWeb or BugLifecycleStatus.BeingWrapped;
        public bool IsReadyToBeHunted => CurrentState.IsReadyToBeHunted;

        public BugEntity(Point startPos, Vector initialVelocity, double size, IBugSpecies species)
        {
            Position = startPos;
            Velocity = initialVelocity;
            Size = size;
            Species = species;
            Angle = Math.Atan2(Velocity.Y, Velocity.X);
            CurrentState = new FlyingBugState();
        }

        public void TransitionTo(IBugBehaviorState nextState, WorldContext world)
        {
            CurrentState = nextState;
            CurrentState.Enter(this, world);
        }

        public void Update(WorldContext world, double dt)
        {
            CurrentState.Update(this, world, dt);
        }

        /// <summary>
        /// Böceği ağdan kurtarır: yarım kalan koza ipi düşer, böcek olduğu yerden rastgele bir yöne uçar
        /// ve kısa bir süre (<see cref="WebImmunityAfterReleaseSeconds"/>) hiçbir ağa yakalanmaz.
        /// Böcek kendi kurtulduğunda da, ağı silindiğinde de bu metot kullanılır.
        /// </summary>
        public void ReleaseFromWeb(WorldContext world)
        {
            TrappedWeb = null;
            WrapProgress = 0.0;
            double angle = world.Random.NextDouble() * Math.PI * 2;
            Vector velocity = new Vector(Math.Cos(angle), Math.Sin(angle)) * Species.MinSpeed;
            SetMotion(Position, velocity, angle);
            TransitionTo(new FlyingBugState(WebImmunityAfterReleaseSeconds), world);
        }

        public void SetMotion(Point position, Vector velocity, double angle)
        {
            Position = position;
            Velocity = velocity;
            Angle = angle;
        }

        public void AdvanceWaves(double wingDelta, double glowDelta)
        {
            WingPhase += wingDelta;
            GlowPhase += glowDelta;
        }

        public void ResetWings()
        {
            WingPhase = 0;
        }
    }
}
