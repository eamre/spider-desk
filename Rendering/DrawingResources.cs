using System.Windows.Media;

namespace SpiderDesk.Rendering
{
    public static class DrawingResources
    {
        public static Pen CreateFrozenPen(Color color, double thickness)
        {
            var brush = CreateFrozenBrush(color);
            var pen = new Pen(brush, thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            pen.Freeze();
            return pen;
        }

        public static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
