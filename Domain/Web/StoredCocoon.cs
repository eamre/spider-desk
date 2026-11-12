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
}
