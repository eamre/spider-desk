using SpiderDesk.Domain.Insects;

namespace SpiderDesk.Core
{
    public enum BugLifecycleStatus
    {
        ActiveInWorld,
        TrappedInWeb,
        BeingWrapped,
        Eaten
    }

    /// <summary>
    /// Böcek davranış durumları için State Pattern arayüzü (OCP & SRP).
    /// </summary>
    public interface IBugBehaviorState
    {
        BugLifecycleStatus Status { get; }
        bool IsReadyToBeHunted { get; }
        void Enter(BugEntity bug, WorldContext world);
        void Update(BugEntity bug, WorldContext world, double dt);
    }
}
