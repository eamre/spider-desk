using System;
using System.Collections.Generic;
using System.Windows;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Domain.Web
{
    /// <summary>
    /// Ağ üzerindeki "Kiler / Erzak Deposu" kozalarını ve depolama noktalarını yönetir.
    /// </summary>
    public class WebPantryController
    {
        private readonly List<StoredCocoon> _storedCocoons = new();

        public IReadOnlyList<StoredCocoon> StoredCocoons => _storedCocoons;
        public bool HasStoredFood => _storedCocoons.Count > 0;

        /// <summary>
        /// Ağın açısal dilimi (StartAngle..EndAngle) içinde uygun bir kiler asma noktası seçer.
        /// Köşe ağlarında kozaların ekran dışına taşmasını ve mevcut kozaların üst üste binmesini önler.
        /// </summary>
        public Point PickStoragePoint(Point webCenter, double webRadius, double startAngle, double endAngle, Random rng)
        {
            double span = endAngle - startAngle;
            double requiredClearanceSq = SpiderTuning.CocoonClearanceDistanceSquared;

            Point bestFallbackPoint = new Point(webCenter.X, webCenter.Y);
            double bestMinDistSq = -1.0;

            for (int attempt = 0; attempt < 40; attempt++)
            {
                double angle;
                if (span >= Math.PI * 1.9)
                {
                    // Tam daire (orb) ağlarda tüm çevreyi (360°) dengeli biçimde kullan
                    angle = rng.NextDouble() * Math.PI * 2.0;
                }
                else
                {
                    // Köşe yelpaze ağlarında duvar kenarlarından emniyetli mesafede geniş yay
                    angle = startAngle + span * (0.12 + rng.NextDouble() * 0.76);
                }

                // Tek bir dar çember yerine iç, orta ve dış kiler katmanlarına kademeli dağıt
                double dist = webRadius * (SpiderTuning.PantryInnerRadiusRatio + rng.NextDouble() * SpiderTuning.PantryOuterRadiusSpan);
                Point candidate = new Point(
                    webCenter.X + Math.Cos(angle) * dist,
                    webCenter.Y + Math.Sin(angle) * dist
                );

                if (_storedCocoons.Count == 0)
                {
                    return candidate;
                }

                double minDistSq = double.MaxValue;
                for (int i = 0; i < _storedCocoons.Count; i++)
                {
                    double dSq = (_storedCocoons[i].Position - candidate).LengthSquared;
                    if (dSq < minDistSq)
                    {
                        minDistSq = dSq;
                    }
                }

                // İstenen 28 piksel emniyet mesafesini sağlayan ilk adayı anında seç
                if (minDistSq >= requiredClearanceSq)
                {
                    return candidate;
                }

                // Çok fazla koza birikmişse, mevcut kozalardan en uzakta kalan (en ferah) noktayı hafızada tut
                if (minDistSq > bestMinDistSq)
                {
                    bestMinDistSq = minDistSq;
                    bestFallbackPoint = candidate;
                }
            }

            return bestFallbackPoint;
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
