using System;
using System.Collections.Generic;
using System.Windows;

namespace SpiderDesk.Domain.Web
{
    public readonly record struct WebSegment(Point Start, Point End, bool IsAnchor);

    public readonly record struct WebBuildStep(Point From, Point To, bool DrawSilk, bool IsAnchor);

    /// <summary>
    /// Dairesel (Orb) ve Köşe Yelpaze (Corner Fan) ağlarının geometrik inşa planlarını üretir.
    /// </summary>
    public static class WebBlueprintGenerator
    {
        public static Queue<WebBuildStep> CreateOrbPlan(Point center, double radius, Random rng)
        {
            var queue = new Queue<WebBuildStep>();
            int spokeCount = rng.Next(10, 14);
            int ringCount = rng.Next(6, 9);

            double[] angles = new double[spokeCount];
            double[] spokeRadii = new double[spokeCount];

            for (int i = 0; i < spokeCount; i++)
            {
                double baseAngle = (Math.PI * 2.0 * i) / spokeCount;
                double jitter = (rng.NextDouble() - 0.5) * (Math.PI * 2.0 / spokeCount) * 0.22;
                angles[i] = baseAngle + jitter;
                spokeRadii[i] = radius * (0.88 + rng.NextDouble() * 0.24);
            }

            // 1. AŞAMA: Ana taşıyıcı teller (Radyal kollar + dış çerçeve)
            Point firstTip = PolarToCartesian(center, angles[0], spokeRadii[0]);
            queue.Enqueue(new WebBuildStep(center, firstTip, DrawSilk: true, IsAnchor: true));

            for (int i = 0; i < spokeCount; i++)
            {
                int nextIdx = (i + 1) % spokeCount;
                Point outerTip = PolarToCartesian(center, angles[i], spokeRadii[i]);
                Point nextOuterTip = PolarToCartesian(center, angles[nextIdx], spokeRadii[nextIdx]);

                queue.Enqueue(new WebBuildStep(outerTip, nextOuterTip, DrawSilk: true, IsAnchor: true));

                if (i < spokeCount - 1)
                {
                    queue.Enqueue(new WebBuildStep(nextOuterTip, center, DrawSilk: true, IsAnchor: true));
                    queue.Enqueue(new WebBuildStep(center, nextOuterTip, DrawSilk: false, IsAnchor: true));
                }
                else
                {
                    queue.Enqueue(new WebBuildStep(nextOuterTip, center, DrawSilk: false, IsAnchor: true));
                }
            }

            // 2. AŞAMA: İçten dışa sarmal (Yakalayıcı yapışkan ipler)
            Point prevSpiralPoint = center;
            int totalSteps = spokeCount * ringCount;

            for (int step = 1; step <= totalSteps; step++)
            {
                int spokeIdx = step % spokeCount;
                double progress = (double)step / totalSteps;
                double r = spokeRadii[spokeIdx] * (0.12 + 0.82 * Math.Pow(progress, 1.08));

                Point nextSpiralPoint = PolarToCartesian(center, angles[spokeIdx], r);
                queue.Enqueue(new WebBuildStep(prevSpiralPoint, nextSpiralPoint, DrawSilk: step > 1, IsAnchor: false));
                prevSpiralPoint = nextSpiralPoint;
            }

            queue.Enqueue(new WebBuildStep(prevSpiralPoint, center, DrawSilk: false, IsAnchor: false));
            return queue;
        }

        /// <summary>
        /// Ekran köşeleri için 90 derecelik yelpaze/üçgen şeklinde kesintisiz köşe ağı planı üretir.
        /// </summary>
        public static Queue<WebBuildStep> CreateCornerFanPlan(
            Point cornerVertex,
            Point restingHub,
            double radius,
            double startAngle,
            double endAngle,
            Random rng)
        {
            var queue = new Queue<WebBuildStep>();
            int spokeCount = rng.Next(7, 10);
            int arcRingCount = rng.Next(7, 10);

            double[] angles = new double[spokeCount];
            double[] spokeRadii = new double[spokeCount];
            double totalSpan = endAngle - startAngle;

            for (int i = 0; i < spokeCount; i++)
            {
                double t = (double)i / (spokeCount - 1);
                double baseAngle = startAngle + totalSpan * t;
                // İlk ve son kol tam duvar kenarına paralel olsun, ara kollarda doğal sapma olsun
                double jitter = (i == 0 || i == spokeCount - 1)
                    ? 0.0
                    : (rng.NextDouble() - 0.5) * (totalSpan / spokeCount) * 0.20;

                angles[i] = baseAngle + jitter;
                // Köşe duvarlarına yakın kollar biraz daha uzun, orta kavis biraz daha doğal
                double edgeStretch = 1.0 + Math.Abs(t - 0.5) * 0.25;
                spokeRadii[i] = radius * edgeStretch * (0.92 + rng.NextDouble() * 0.16);
            }

            // 1. AŞAMA: Köşe noktasından dışa doğru yelpaze kolları ve dış kemer (ipi hiç bırakmadan)
            Point firstTip = PolarToCartesian(cornerVertex, angles[0], spokeRadii[0]);
            queue.Enqueue(new WebBuildStep(cornerVertex, firstTip, DrawSilk: true, IsAnchor: true));

            for (int i = 0; i < spokeCount - 1; i++)
            {
                Point outerTip = PolarToCartesian(cornerVertex, angles[i], spokeRadii[i]);
                Point nextOuterTip = PolarToCartesian(cornerVertex, angles[i + 1], spokeRadii[i + 1]);

                // Dış kemer telini yan kola çek
                queue.Enqueue(new WebBuildStep(outerTip, nextOuterTip, DrawSilk: true, IsAnchor: true));

                // Yan koldan köşe merkezine ipi hiç bırakmadan in ve üçgeni kapat
                queue.Enqueue(new WebBuildStep(nextOuterTip, cornerVertex, DrawSilk: true, IsAnchor: true));

                if (i < spokeCount - 2)
                {
                    // Sıradaki dilim için ördüğü koldan tekrar uca tırman
                    queue.Enqueue(new WebBuildStep(cornerVertex, nextOuterTip, DrawSilk: false, IsAnchor: true));
                }
            }

            // 2. AŞAMA: İçten dışa zikzak yelpaze kemerleri (Bir duvardan diğer duvara kesintisiz dokuma)
            Point prevPoint = cornerVertex;
            for (int ring = 1; ring <= arcRingCount; ring++)
            {
                double progress = (double)ring / (arcRingCount + 1);
                double ringRatio = 0.16 + 0.78 * Math.Pow(progress, 1.05);
                bool leftToRight = (ring % 2 == 1);

                for (int step = 0; step < spokeCount; step++)
                {
                    int spokeIdx = leftToRight ? step : (spokeCount - 1 - step);
                    double r = spokeRadii[spokeIdx] * ringRatio;
                    Point nextPoint = PolarToCartesian(cornerVertex, angles[spokeIdx], r);

                    // Aynı kol üzerinde bir üst halkaya çıkarken ana telin üstünden yürür,
                    // kollar arasında geçerken yakalayıcı yelpaze telini örer
                    bool isCrossingSpokes = (step > 0);
                    queue.Enqueue(new WebBuildStep(prevPoint, nextPoint, DrawSilk: isCrossingSpokes, IsAnchor: false));
                    prevPoint = nextPoint;
                }
            }

            // 3. AŞAMA: Ağ tamamlandığında köşenin hemen içindeki nöbet noktasına (restingHub) geç
            queue.Enqueue(new WebBuildStep(prevPoint, restingHub, DrawSilk: false, IsAnchor: false));
            return queue;
        }

        public static Point PolarToCartesian(Point center, double angle, double radius)
        {
            return new Point(
                center.X + Math.Cos(angle) * radius,
                center.Y + Math.Sin(angle) * radius
            );
        }
    }
}
