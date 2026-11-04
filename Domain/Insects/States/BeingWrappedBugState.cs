using System.Windows;

namespace SpiderDesk.Domain.Insects.States
{
    /// <summary>
    /// Böceğin örümcek tarafından ipek kozayla sarıldığı ve yenildiği durum (OCP & SRP).
    /// </summary>
    public class BeingWrappedBugState : IBugBehaviorState
    {
        private Point _anchor;
        private bool _isEaten;

        public BugLifecycleStatus Status => _isEaten ? BugLifecycleStatus.Eaten : BugLifecycleStatus.BeingWrapped;
        public bool IsReadyToBeHunted => false;

        public void Enter(BugEntity bug, WorldContext world)
        {
            _anchor = bug.Position;
        }

        public void Update(BugEntity bug, WorldContext world, double dt)
        {
            bug.AdvanceWaves(dt * 40.0 * (1.0 - bug.WrapProgress), dt * 2.0);
            bug.SetMotion(_anchor, new Vector(0, 0), bug.Angle + dt * 10.0);

            if (bug.WrapProgress >= 1.0)
            {
                _isEaten = true;
            }
        }
    }
}
