using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;

namespace SpiderDesk.Infrastructure
{
    /// <summary>
    /// Tıklama geçirgenliğini (Click-Through) bozmadan fare imlecinin ekran konumunu ve hızını takip eder (SRP & DIP).
    /// </summary>
    public class CursorTrackingService : ICursorTrackingService
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Win32Point pt);

        public Point CurrentPosition { get; private set; }
        public Point PreviousPosition { get; private set; }
        public double Speed { get; private set; }
        private bool _isInitialized;

        public void Update(Visual referenceVisual, double dt)
        {
            if (!GetCursorPos(out Win32Point rawPt)) return;

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
