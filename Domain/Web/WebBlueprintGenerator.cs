using System;
using System.Collections.Generic;
using System.Windows;

namespace SpiderDesk.Domain.Web
{
    public readonly record struct WebSegment(Point Start, Point End, bool IsAnchor);

    public readonly record struct WebBuildStep(Point From, Point To, bool DrawSilk, bool IsAnchor);

    /// <summary>
    /// Sadece örümcek ağının geometrik inşa planını üretmekten sorumludur (SRP).
    /// </summary>
    public static class WebBlueprintGenerator
    {
        public static Queue<WebBuildStep> CreatePlan(Point center, double radius, Random rng)
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
            // İlk ana kolu merkezden 0. uca çek
            Point firstTip = PolarToCartesian(center, angles[0], spokeRadii[0]);
            queue.Enqueue(new WebBuildStep(center, firstTip, DrawSilk: true, IsAnchor: true));

            for (int i = 0; i < spokeCount; i++)
            {
                int nextIdx = (i + 1) % spokeCount;
                Point outerTip = PolarToCartesian(center, angles[i], spokeRadii[i]);
                Point nextOuterTip = PolarToCartesian(center, angles[nextIdx], spokeRadii[nextIdx]);

                // Dış çerçeve telini yan uca çek
                queue.Enqueue(new WebBuildStep(outerTip, nextOuterTip, DrawSilk: true, IsAnchor: true));

                if (i < spokeCount - 1)
                {
                    // İpi HİÇ BIRAKMADAN yan uçtan doğrudan merkeze çekerek üçgeni kapat!
                    queue.Enqueue(new WebBuildStep(nextOuterTip, center, DrawSilk: true, IsAnchor: true));

                    // Az önce ördüğü telin üzerinden tekrar uca tırmanıp sıradaki dilime devam et
                    queue.Enqueue(new WebBuildStep(center, nextOuterTip, DrawSilk: false, IsAnchor: true));
                }
                else
                {
                    // Son dilimde 0. tel zaten örülü olduğu için onun üzerinden merkeze in
                    queue.Enqueue(new WebBuildStep(nextOuterTip, center, DrawSilk: false, IsAnchor: true));
                }
            }

            // 2. AŞAMA: İçten dışa sarmal (Yakalayıcı yapışkan ipler - kesintisiz tek ip)
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

            // 3. AŞAMA: Bitince merkeze geri yürü
            queue.Enqueue(new WebBuildStep(prevSpiralPoint, center, DrawSilk: false, IsAnchor: false));
            return queue;
        }

        private static Point PolarToCartesian(Point center, double angle, double radius)
        {
            return new Point(
                center.X + Math.Cos(angle) * radius,
                center.Y + Math.Sin(angle) * radius
            );
        }
    }
}
