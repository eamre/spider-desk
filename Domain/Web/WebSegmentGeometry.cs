using System;
using System.Windows;

namespace SpiderDesk.Domain.Web
{
    /// <summary>
    /// Ağ tellerinin içbükey (scalloped) Bezier kavis ve çiy sarkma matematiğini tek merkezde toplar.
    /// Hem çizim katmanı (WebRenderer) hem de damla konumlandırması (WebDropletController) aynı hesabı kullanır.
    /// </summary>
    public static class WebSegmentGeometry
    {
        public static Point ComputeControlPoint(Point start, Point end, bool isAnchor, Point webCenter, double dropletSag)
        {
            Point mid = new Point((start.X + end.X) * 0.5, (start.Y + end.Y) * 0.5);
            if (isAnchor)
            {
                return new Point(mid.X, mid.Y + dropletSag);
            }

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double segLen = Math.Sqrt(dx * dx + dy * dy);

            double toCenterX = webCenter.X - mid.X;
            double toCenterY = webCenter.Y - mid.Y;
            double distToCenter = Math.Sqrt(toCenterX * toCenterX + toCenterY * toCenterY);

            if (distToCenter > 0.001)
            {
                toCenterX /= distToCenter;
                toCenterY /= distToCenter;
            }

            double inwardPull = Math.Min(11.0, segLen * 0.13);
            return new Point(
                mid.X + toCenterX * inwardPull * 1.8,
                mid.Y + toCenterY * inwardPull * 1.8 + dropletSag * 1.6 + 0.6
            );
        }

        public static Point EvaluateQuadraticBezier(Point p0, Point control, Point p2, double t)
        {
            double inv = 1.0 - t;
            double w0 = inv * inv;
            double w1 = 2.0 * inv * t;
            double w2 = t * t;
            return new Point(
                w0 * p0.X + w1 * control.X + w2 * p2.X,
                w0 * p0.Y + w1 * control.Y + w2 * p2.Y
            );
        }
    }
}
