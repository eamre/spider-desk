using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain.Spider.Themes;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Sadece örümcek ağlarını, rüzgar/titreşim dalgalarını, su damlaları ağırlığıyla esneyen telleri
    /// ve düşen su damlalarını aktif temaya göre çizer (SRP).
    /// </summary>
    public class WebRenderer : IRenderer<SpiderWebEntity>
    {
        private readonly Pen _brokenStubPen = DrawingResources.CreateFrozenPen(Color.FromArgb(150, 255, 210, 210), 1.0);
        private readonly Pen _cocoonSilkPen = DrawingResources.CreateFrozenPen(Color.FromArgb(225, 255, 255, 255), 1.2);
        private readonly SolidColorBrush _cocoonFillBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(230, 242, 248, 255));
        private readonly SolidColorBrush _dewBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(180, 255, 255, 255));
        private readonly SolidColorBrush _waterDropBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(205, 195, 238, 255));
        private readonly SolidColorBrush _waterHighlightBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(245, 255, 255, 255));
        private readonly Dictionary<byte, SolidColorBrush> _fallingDropBrushes = new();

        private Pen _anchorPen = null!;
        private Pen _spiralPen = null!;

        public WebRenderer()
        {
            ApplyTheme(SpiderThemeCatalog.All[0]);
        }

        public void ApplyTheme(ISpiderTheme theme)
        {
            _anchorPen = DrawingResources.CreateFrozenPen(theme.WebAnchorColor, 1.35);
            _spiralPen = DrawingResources.CreateFrozenPen(theme.WebSpiralColor, 0.95);
        }

        public void Render(DrawingContext dc, SpiderWebEntity web)
        {
            bool vibrating = web.VibrationAmplitude > 0.05;

            for (int i = 0; i < web.CompletedSegments.Count; i++)
            {
                var seg = web.CompletedSegments[i];
                Point p1 = seg.Start;
                Point p2 = seg.End;

                double windOffset = 0;
                if (!seg.IsAnchor && web.WindStrength > 0.05)
                {
                    windOffset = Math.Sin(web.WindPhase + i * 0.35) * web.WindStrength;
                    p1.X += windOffset * 0.65;
                    p1.Y += windOffset * 0.35;
                    p2.X += windOffset * 0.65;
                    p2.Y += windOffset * 0.35;
                }

                if (vibrating)
                {
                    Point mid = new Point((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
                    double dist = (mid - web.VibrationOrigin).Length;
                    double falloff = Math.Max(0, 1.0 - dist / (web.Radius * 1.5));
                    double wave = Math.Sin(web.VibrationPhase - dist * 0.08) * web.VibrationAmplitude * falloff;

                    p1.X += wave * 0.5;
                    p1.Y += wave * 0.5;
                    p2.X -= wave * 0.5;
                    p2.Y -= wave * 0.5;
                }

                Pen pen = seg.IsAnchor ? _anchorPen : _spiralPen;
                double sag = web.Droplets.GetSegmentSag(i);

                if (!seg.IsAnchor)
                {
                    // Yakalayıcı ipek telleri iki radyal kol arasında merkeze doğru hafif içbükey (scalloped)
                    // kavis ve yerçekimi esnemesiyle çizerek doğal örümcek ağı estetiği oluştur
                    DrawScallopedSilkSegment(dc, pen, p1, p2, web.Center, sag);
                }
                else if (sag > 0.25)
                {
                    Point midSag = new Point(
                        (p1.X + p2.X) * 0.5,
                        (p1.Y + p2.Y) * 0.5 + sag
                    );
                    dc.DrawLine(pen, p1, midSag);
                    dc.DrawLine(pen, midSag, p2);
                }
                else
                {
                    dc.DrawLine(pen, p1, p2);
                }

                if (!seg.IsAnchor && i % 6 == 0)
                {
                    dc.DrawEllipse(_dewBrush, null, p2, 1.3, 1.3);
                }
            }

            // 2. Tellerin üzerindeki ağır su/yağmur damlalarını çiz
            foreach (var drop in web.Droplets.AttachedDroplets)
            {
                Point pos = web.Droplets.CalculateDropletWorldPosition(drop, web.CompletedSegments, web.Center);
                double r = drop.Radius;
                dc.DrawEllipse(_waterDropBrush, null, pos, r * 0.92, r * 1.12);
                dc.DrawEllipse(_waterHighlightBrush, null, new Point(pos.X - r * 0.28, pos.Y - r * 0.3), r * 0.32, r * 0.32);
            }

            // 3. Örümcek ağı silkelediğinde aşağı düşen su damlalarını çiz
            foreach (var falling in web.Droplets.FallingDroplets)
            {
                byte alpha = (byte)((int)(falling.Opacity * 200) / 10 * 10);
                if (alpha < 10) continue;

                var brush = GetFallingDropBrush(alpha);
                double r = falling.Radius;
                dc.DrawEllipse(brush, null, falling.FallingPosition, r * 0.78, r * 1.35);
            }

            // 4. Kopmuş tellerin sarkan uçları
            foreach (var broken in web.DamagedSegments)
            {
                Vector dir = broken.End - broken.Start;
                Point danglingTip = new Point(
                    broken.Start.X + dir.X * 0.25,
                    broken.Start.Y + dir.Y * 0.25 + 6.0 + Math.Sin(web.WindPhase * 2.0) * 2.0
                );
                dc.DrawLine(_brokenStubPen, broken.Start, danglingTip);
            }

            // 5. Kilerde (Erzak Deposunda) asılı duran ipek kozalar
            foreach (var cocoon in web.Pantry.StoredCocoons)
            {
                double scale = Math.Max(0.2, 1.0 - cocoon.ConsumeProgress * 0.85);
                double r = cocoon.Size * 1.15 * scale;

                dc.PushTransform(new TranslateTransform(cocoon.Position.X, cocoon.Position.Y));
                dc.PushTransform(new RotateTransform(cocoon.Angle * 180.0 / Math.PI));
                dc.DrawEllipse(_cocoonFillBrush, _cocoonSilkPen, new Point(0, 0), r * 1.2, r * 0.82);
                dc.DrawLine(_cocoonSilkPen, new Point(-r * 0.7, -r * 0.5), new Point(r * 0.7, r * 0.5));
                dc.DrawLine(_cocoonSilkPen, new Point(-r * 0.7, r * 0.5), new Point(r * 0.7, -r * 0.5));
                dc.Pop();
                dc.Pop();
            }
        }

        private static void DrawScallopedSilkSegment(
            DrawingContext dc,
            Pen pen,
            Point p1,
            Point p2,
            Point webCenter,
            double dropletSag)
        {
            Point mid = new Point((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
            double segLen = (p2 - p1).Length;

            Vector toCenter = webCenter - mid;
            double distToCenter = toCenter.Length;
            if (distToCenter > 0.001)
            {
                toCenter /= distToCenter;
            }

            // İki radyal kol arasındaki ipek tel merkeze doğru hafif içbükey gerilir
            double inwardPull = Math.Min(11.0, segLen * 0.13);
            Point control = new Point(
                mid.X + toCenter.X * inwardPull * 1.8,
                mid.Y + toCenter.Y * inwardPull * 1.8 + dropletSag * 1.6 + 0.6
            );

            // Kuadratik Bezier eğrisini 4 hafif doğru parçasıyla (sıfır bellek tahsisiyle) pürüzsüz çiz
            Point q1 = EvalQuadBezier(p1, control, p2, 0.25);
            Point q2 = EvalQuadBezier(p1, control, p2, 0.50);
            Point q3 = EvalQuadBezier(p1, control, p2, 0.75);

            dc.DrawLine(pen, p1, q1);
            dc.DrawLine(pen, q1, q2);
            dc.DrawLine(pen, q2, q3);
            dc.DrawLine(pen, q3, p2);
        }

        private static Point EvalQuadBezier(Point p0, Point c, Point p2, double t)
        {
            double inv = 1.0 - t;
            double w0 = inv * inv;
            double w1 = 2.0 * inv * t;
            double w2 = t * t;
            return new Point(
                w0 * p0.X + w1 * c.X + w2 * p2.X,
                w0 * p0.Y + w1 * c.Y + w2 * p2.Y
            );
        }

        private SolidColorBrush GetFallingDropBrush(byte alpha)
        {
            if (!_fallingDropBrushes.TryGetValue(alpha, out var brush))
            {
                brush = DrawingResources.CreateFrozenBrush(Color.FromArgb(alpha, 195, 238, 255));
                _fallingDropBrushes[alpha] = brush;
            }
            return brush;
        }
    }
}
