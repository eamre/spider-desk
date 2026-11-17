namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Örümcek davranış durumu arayüzü. Her durum kendi kesilebilirlik ve animasyon
    /// yeteneklerini tanımlayarak yeni durumlar eklendiğinde ana varlık sınıfının değişmesini önler.
    /// </summary>
    public interface ISpiderBehaviorState
    {
        string DisplayName { get; }
        bool CanBeStartledByCursor => true;
        bool CanHuntPrey => true;
        bool CanEatFromPantry => true;
        bool CanStartScheduledWeb => false;

        /// <summary>
        /// Örümcek bu durumdayken elindeki işi bırakıp bir mesaj gösterebilir mi (ipekle FOCUS/BREAK yazmak ya da tavandan not göstermek)?
        /// Av taşırken veya ağ örerken hayır: mesaj, örümcek o işi bitirene kadar sırada bekler.
        /// </summary>
        bool CanStopForMessage => false;
        bool IsAlwaysAnimating => true;

        void Enter(SpiderEntity spider, WorldContext world);
        void Update(SpiderEntity spider, WorldContext world, double dt);
        void Exit(SpiderEntity spider, WorldContext world);
    }
}
