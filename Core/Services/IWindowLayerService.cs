using System.Windows;

namespace SpiderDesk.Core.Services
{
    public interface IWindowLayerService
    {
        bool AlwaysOnTop { get; set; }
        bool IsOverlayInteractive { get; }
        bool IsDesktopOccluded { get; }
        void Attach(Window window);
        void EnforceZOrder();
    }
}
