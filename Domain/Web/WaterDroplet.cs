using System.Windows;

namespace SpiderDesk.Domain.Web
{
    /// <summary>
    /// Ağ teli üzerinde biriken veya silkelendiğinde aşağı düşen tek bir su/çiy damlasını temsil eder (SRP).
    /// </summary>
    public class WaterDroplet
    {
        public int SegmentIndex { get; set; }
        public double NormalizedT { get; } // Tel üzerindeki konumu (0.2 .. 0.8)
        public double Radius { get; private set; }
        public bool IsFalling { get; private set; }
        public Point FallingPosition { get; private set; }
        public double FallVelocityY { get; private set; }
        public double Opacity { get; private set; } = 1.0;

        public WaterDroplet(int segmentIndex, double normalizedT, double initialRadius = 2.0)
        {
            SegmentIndex = segmentIndex;
            NormalizedT = normalizedT;
            Radius = initialRadius;
        }

        public void Grow(double amount, double maxRadius = 3.8)
        {
            if (!IsFalling && Radius < maxRadius)
            {
                Radius = System.Math.Min(maxRadius, Radius + amount);
            }
        }

        public void Detach(Point worldStartPosition, double initialVelocityY)
        {
            IsFalling = true;
            FallingPosition = worldStartPosition;
            FallVelocityY = initialVelocityY;
        }

        public void UpdateFalling(double dt)
        {
            if (!IsFalling) return;

            FallVelocityY += 580.0 * dt; // Yerçekimi ivmesi
            FallingPosition = new Point(FallingPosition.X, FallingPosition.Y + FallVelocityY * dt);
            Opacity = System.Math.Max(0.0, Opacity - dt * 1.15);
        }
    }
}
