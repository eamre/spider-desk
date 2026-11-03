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
        /// Ekran köşeleri için 90 derecelik, içbükey kemerli ve kesintisiz köşe yelpaze ağı planı üretir.
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
            int spokeCount = 10;
            int arcRingCount = 10;

            double[] angles = new double[spokeCount];
            double[] spokeRadii = new double[spokeCount];
            double totalSpan = endAngle - startAngle;

            for (int i = 0; i < spokeCount; i++)
            {
                double t = (double)i / (spokeCount - 1);
                double baseAngle = startAngle + totalSpan * t;
                // İlk ve son kol tam ekran kenarlarına paralel uzansın, ara kollarda çok hafif doğal sapma olsun
                double jitter = (i == 0 || i == spokeCount - 1)
                    ? 0.0
                    : (rng.NextDouble() - 0.5) * (totalSpan / spokeCount) * 0.12;

                angles[i] = baseAngle + jitter;

                // Duvar kenarlarında (t=0 ve t=1) uzun, 45 derece diyagonal ortasında (t=0.5) içbükey zarif kavis
                double concaveProfile = 1.24 - 0.36 * Math.Sin(t * Math.PI);
                double subtleVariance = 0.985 + rng.NextDouble() * 0.03;
                spokeRadii[i] = radius * concaveProfile * subtleVariance;
            }

            // 1. AŞAMA: Köşe noktasından dışa doğru radyal yelpaze kolları ve dış kemer (ipi hiç bırakmadan)
            Point firstTip = PolarToCartesian(cornerVertex, angles[0], spokeRadii[0]);
            queue.Enqueue(new WebBuildStep(cornerVertex, firstTip, DrawSilk: true, IsAnchor: true));

            for (int i = 0; i < spokeCount - 1; i++)
            {
                Point outerTip = PolarToCartesian(cornerVertex, angles[i], spokeRadii[i]);
                Point nextOuterTip = PolarToCartesian(cornerVertex, angles[i + 1], spokeRadii[i + 1]);

                // Dış kemer telini yan kola çek (IsAnchor: false ile içbükey ipek kavisi alır)
                queue.Enqueue(new WebBuildStep(outerTip, nextOuterTip, DrawSilk: true, IsAnchor: false));

                // Yan koldan köşe merkezine radyal taşıyıcı kolu çekerek in
                queue.Enqueue(new WebBuildStep(nextOuterTip, cornerVertex, DrawSilk: true, IsAnchor: true));

                if (i < spokeCount - 2)
                {
                    // Sıradaki dilim için ördüğü koldan tekrar uca tırman
                    queue.Enqueue(new WebBuildStep(cornerVertex, nextOuterTip, DrawSilk: false, IsAnchor: true));
                }
            }

            // 2. AŞAMA: İçten dışa logaritmik aralıklı zikzak yelpaze kemerleri (Merkezde sık, dışa doğru genişleyen)
            Point prevPoint = cornerVertex;
            for (int ring = 1; ring <= arcRingCount; ring++)
            {
                double progress = (double)ring / (arcRingCount + 1);
                double ringRatio = 0.10 + 0.82 * Math.Pow(progress, 1.22);
                bool leftToRight = (ring % 2 == 1);

                for (int step = 0; step < spokeCount; step++)
                {
                    int spokeIdx = leftToRight ? step : (spokeCount - 1 - step);
                    double r = spokeRadii[spokeIdx] * ringRatio;
                    Point nextPoint = PolarToCartesian(cornerVertex, angles[spokeIdx], r);

                    bool isCrossingSpokes = (step > 0);
                    queue.Enqueue(new WebBuildStep(prevPoint, nextPoint, DrawSilk: isCrossingSpokes, IsAnchor: false));
                    prevPoint = nextPoint;
                }
            }

            // 3. AŞAMA: Ağ tamamlandığında köşenin içindeki nöbet noktasına (restingHub) geç
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
