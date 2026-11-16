using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace SpiderDesk.Infrastructure.Tray
{
    /// <summary>
    /// Sistem tepsisinde görünen 32x32 örümcek ikonunu kod ile çizer.
    /// </summary>
    public static class TrayIconFactory
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Icon CreateSpiderIcon()
        {
            using Bitmap bmp = new Bitmap(32, 32);
            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var legPen = new Pen(Color.FromArgb(240, 30, 30, 35), 2.2f);
            g.DrawLines(legPen, new[] { new Point(16, 15), new Point(6, 7), new Point(2, 3) });
            g.DrawLines(legPen, new[] { new Point(16, 15), new Point(26, 7), new Point(30, 3) });
            g.DrawLines(legPen, new[] { new Point(16, 16), new Point(4, 13), new Point(1, 12) });
            g.DrawLines(legPen, new[] { new Point(16, 16), new Point(28, 13), new Point(31, 12) });
            g.DrawLines(legPen, new[] { new Point(16, 17), new Point(5, 20), new Point(2, 23) });
            g.DrawLines(legPen, new[] { new Point(16, 17), new Point(27, 20), new Point(30, 23) });
            g.DrawLines(legPen, new[] { new Point(16, 18), new Point(7, 25), new Point(4, 30) });
            g.DrawLines(legPen, new[] { new Point(16, 18), new Point(25, 25), new Point(28, 30) });

            g.FillEllipse(Brushes.Black, 10, 16, 12, 13);
            g.FillEllipse(Brushes.Black, 12, 10, 8, 8);
            g.FillEllipse(Brushes.Crimson, 14, 20, 4, 5);
            g.FillEllipse(Brushes.Red, 13, 11, 2, 2);
            g.FillEllipse(Brushes.Red, 17, 11, 2, 2);

            // GetHicon yönetilmeyen bir tutamaç üretir; kopyasını alıp aslını hemen serbest bırakırız
            IntPtr hIcon = bmp.GetHicon();
            try
            {
                using Icon wrapped = Icon.FromHandle(hIcon);
                return (Icon)wrapped.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
    }
}
