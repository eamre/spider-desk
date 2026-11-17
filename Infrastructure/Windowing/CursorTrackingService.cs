using System.Windows;
using System.Windows.Media;
using static SpiderDesk.Infrastructure.Windowing.NativeMethods;

namespace SpiderDesk.Infrastructure.Windowing
{
    /// <summary>
    /// Tıklama geçirgenliğini (Click-Through) bozmadan fare imlecinin ekran konumunu ve hızını takip eder.
    /// </summary>
    public class CursorTrackingService : ICursorTrackingService
    {
        public Point CurrentPosition { get; private set; }
        public Point PreviousPosition { get; private set; }
        public double Speed { get; private set; }
        private bool _isInitialized;

        public void Update(Visual referenceVisual, double dt)
        {
            if (!GetCursorPos(out POINT rawPt)) return;

            Point screenPt = new Point(rawPt.X, rawPt.Y);

            // Yüksek DPI ekran ölçeklendirmesi (125%, 150% vb.) varsa WPF koordinatına dönüştür
            PresentationSource? source = PresentationSource.FromVisual(referenceVisual);
            if (source?.CompositionTarget != null)
            {
                Matrix transform = source.CompositionTarget.TransformFromDevice;
                screenPt = transform.Transform(screenPt);
            }

            if (!_isInitialized)
            {
                CurrentPosition = screenPt;
                PreviousPosition = screenPt;
                Speed = 0;
                _isInitialized = true;
                return;
            }

            PreviousPosition = CurrentPosition;
            CurrentPosition = screenPt;

            if (dt > 0.0001)
            {
                Speed = (CurrentPosition - PreviousPosition).Length / dt;
            }
        }
    }
}
