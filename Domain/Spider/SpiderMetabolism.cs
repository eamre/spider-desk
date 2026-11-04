using System;

namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Örümceğin tokluk/enerji seviyesini (0 - 100) ve kalori harcamasını yönetir (SRP).
    /// </summary>
    public class SpiderMetabolism
    {
        public double Satiety { get; private set; } = SpiderTuning.InitialSatiety;

        /// <summary>
        /// Tokluk eşik değerinin (%75 ve üzeri) üstündeyse örümcek tamamen doymuştur.
        /// </summary>
        public bool IsFull => Satiety >= SpiderTuning.FullSatietyThreshold;

        /// <summary>
        /// Tokluk açlık eşiğine (%50 ve altı) düştüğünde örümcek acıkır ve erzak deposundaki kozaları yemeye başlar.
        /// </summary>
        public bool IsHungry => Satiety <= SpiderTuning.HungrySatietyThreshold;

        public int SatietyPercent => (int)Math.Round(Satiety);

        public void Update(double dt, double currentMovementSpeed)
        {
            // Dururken bazal metabolizma, hareket ederken ekstra enerji harcar
            double burnRate = SpiderTuning.BasalBurnRatePerSecond + (currentMovementSpeed * SpiderTuning.MovementBurnFactor);
            Satiety = Math.Max(0.0, Satiety - burnRate * dt);
        }

        public void Feed(double nutritionAmount = SpiderTuning.PreyNutrition)
        {
            Satiety = Math.Min(100.0, Satiety + nutritionAmount);
        }
    }
}
