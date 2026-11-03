using System;

namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Örümceğin tokluk/enerji seviyesini (0 - 100) ve kalori harcamasını yönetir (SRP).
    /// </summary>
    public class SpiderMetabolism
    {
        public double Satiety { get; private set; } = 70.0; // Başlangıçta %70 tok

        /// <summary>
        /// Tokluk %75 ve üzerindeyse örümcek tamamen doymuştur.
        /// </summary>
        public bool IsFull => Satiety >= 75.0;

        /// <summary>
        /// Tokluk %50 ve altına düştüğünde örümcek acıkır ve erzak deposundaki kozaları yemeye başlar.
        /// </summary>
        public bool IsHungry => Satiety <= 50.0;

        public int SatietyPercent => (int)Math.Round(Satiety);

        public void Update(double dt, double currentMovementSpeed)
        {
            // Dururken bazal metabolizma (%0.65/sn), hareket ederken ekstra enerji harcar
            double burnRate = 0.65 + (currentMovementSpeed * 0.0045);
            Satiety = Math.Max(0.0, Satiety - burnRate * dt);
        }

        public void Feed(double nutritionAmount = 25.0)
        {
            Satiety = Math.Min(100.0, Satiety + nutritionAmount);
        }
    }
}
