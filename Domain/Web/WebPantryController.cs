using System;
using System.Collections.Generic;
using System.Windows;

namespace SpiderDesk.Domain.Web
{
    public class StoredCocoon
    {
        public Point Position { get; }
        public double Size { get; }
        public double Angle { get; }
        public double ConsumeProgress { get; set; } // 0.0 -> 1.0 (Örümcek kilerden yerken küçülür)

        public StoredCocoon(Point position, double size, double angle)
        {
            Position = position;
            Size = size;
            Angle = angle;
        }
    }

    /// <summary>
    /// Ağ üzerindeki "Kiler / Erzak Deposu" kozalarını ve depolama noktalarını yönetir (SRP).
    /// </summary>
    public class WebPantryController
    {
        private readonly List<StoredCocoon> _storedCocoons = new();

        public IReadOnlyList<StoredCocoon> StoredCocoons => _storedCocoons;
        public bool HasStoredFood => _storedCocoons.Count > 0;

        /// <summary>
        /// Ağın açısal dilimi (StartAngle..EndAngle) içinde uygun bir kiler asma noktası seçer.
        /// Köşe ağlarında kozaların ekran dışına taşmasını önler.
        /// </summary>
        public Point PickStoragePoint(Point webCenter, double webRadius, double startAngle, double endAngle, Random rng)
        {
            double span = endAngle - startAngle;
            double angle;

            if (span >= Math.PI * 1.9)
            {
                // Tam daire ağlarda üst yarıyı tercih et
                angle = -Math.PI * (0.22 + rng.NextDouble() * 0.56);
            }
            else
            {
                // Köşe yelpaze ağlarında yelpazenin iç dilimini (duvarlardan biraz içeriyi) seç
                angle = startAngle + span * (0.18 + rng.NextDouble() * 0.64);
            }

            double dist = webRadius * (0.55 + rng.NextDouble() * 0.22);

            return new Point(
                webCenter.X + Math.Cos(angle) * dist,
                webCenter.Y + Math.Sin(angle) * dist
            );
        }

        public void StoreCocoon(Point position, double size, double angle)
        {
            _storedCocoons.Add(new StoredCocoon(position, size, angle));
        }

        /// <summary>
        /// Erzağa ilk eklenen (en eski) kozayı döndürür (FIFO).
        /// Böylece yeni yakalanan böcek erzağa eklendiğinde örümcek yeni ekleneni değil,
        /// erzakta bekleyen eski kozaları sırayla yer.
        /// </summary>
        public StoredCocoon? PeekNextMeal()
        {
            return _storedCocoons.Count > 0 ? _storedCocoons[0] : null;
        }

        public void RemoveCocoon(StoredCocoon cocoon)
        {
            _storedCocoons.Remove(cocoon);
        }
    }
}
