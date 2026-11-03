using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Core
{
    /// <summary>
    /// State Pattern arayüzü (Open/Closed Principle):
    /// Yeni bir örümcek davranışı eklemek için SpiderEntity sınıfını değiştirmeden
    /// bu arayüzü uygulayan yeni bir sınıf eklemek yeterlidir.
    /// </summary>
    public interface ISpiderBehaviorState
    {
        string DisplayName { get; }
        void Enter(SpiderEntity spider, WorldContext world);
        void Update(SpiderEntity spider, WorldContext world, double dt);
        void Exit(SpiderEntity spider, WorldContext world);
    }
}
