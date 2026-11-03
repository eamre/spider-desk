using System.Windows;
using System.Windows.Media;

namespace SpiderDesk.Core
{
    public interface IWindowLayerService
    {
        bool AlwaysOnTop { get; set; }
        bool IsOverlayInteractive { get; }
        void Attach(Window window);
        void EnforceZOrder();
    }

    public interface ICursorTrackingService
    {
        Point CurrentPosition { get; }
        Point PreviousPosition { get; }
        double Speed { get; }
        void Update(Visual referenceVisual, double dt);
    }

    public interface IStartupRegistrationService
    {
        bool IsEnabled();
        void SetEnabled(bool enable);
    }
}
