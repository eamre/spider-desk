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
        /// Ağın dış-üst çemberine yakın uygun bir kiler asma noktası seçer.
        /// </summary>
        public Point PickStoragePoint(Point webCenter, double webRadius, Random rng)
        {
            // Üst yarıdaki (-150° ile -30° arası) dış halkalardan bir nokta seç
            double baseAngle = -Math.PI * (0.22 + rng.NextDouble() * 0.56);
            double dist = webRadius * (0.58 + rng.NextDouble() * 0.22);

            return new Point(
                webCenter.X + Math.Cos(baseAngle) * dist,
                webCenter.Y + Math.Sin(baseAngle) * dist
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
