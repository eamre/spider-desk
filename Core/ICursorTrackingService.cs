using System.Windows;
using System.Windows.Media;

namespace SpiderDesk.Core
{
    public interface ICursorTrackingService
    {
        Point CurrentPosition { get; }
        Point PreviousPosition { get; }
        double Speed { get; }
        void Update(Visual referenceVisual, double dt);
    }
}
