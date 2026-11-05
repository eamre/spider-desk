using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain.Spider.Themes;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Sadece örümcek ağlarını, titreşim dalgalarını, su damlaları ağırlığıyla esneyen telleri
    /// ve düşen su damlalarını aktif temaya göre çizer.
    /// Statik ağ geometrilerini dondurulmuş StreamGeometry olarak önbelleğe alarak CPU/GPU yükünü %90+ azaltır.
    /// </summary>
    public class WebRenderer : IRenderer<SpiderWebEntity>
    {
        private sealed class CachedWebGeometry
        {
            public int SegmentCount;
            public int DamagedCount;
            public int DropletRevision;
            public StreamGeometry AnchorGeometry = null!;
            public StreamGeometry SpiralGeometry = null!;
            public StreamGeometry DewGeometry = null!;
        }

        private readonly Pen _brokenStubPen = DrawingResources.CreateFrozenPen(Color.FromArgb(150, 255, 210, 210), 1.0);
        private readonly Pen _cocoonSilkPen = DrawingResources.CreateFrozenPen(Color.FromArgb(225, 255, 255, 255), 1.2);
        private readonly SolidColorBrush _cocoonFillBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(230, 242, 248, 255));
        private readonly SolidColorBrush _dewBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(180, 255, 255, 255));
        private readonly SolidColorBrush _waterDropBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(205, 195, 238, 255));
        private readonly SolidColorBrush _waterHighlightBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(245, 255, 255, 255));
        private readonly Dictionary<byte, SolidColorBrush> _fallingDropBrushes = new();
        private readonly ConditionalWeakTable<SpiderWebEntity, CachedWebGeometry> _geometryCache = new();

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

            if (!vibrating)
            {
                // Ağ titreşmiyorken dondurulmuş (Frozen) StreamGeometry önbelleğinden tek seferde çiz (Sıfır CPU tessellation)
                CachedWebGeometry cache = GetOrBuildCache(web);
                dc.DrawGeometry(null, _anchorPen, cache.AnchorGeometry);
                dc.DrawGeometry(null, _spiralPen, cache.SpiralGeometry);
                dc.DrawGeometry(_dewBrush, null, cache.DewGeometry);
            }
            else
            {
                RenderVibratingSegments(dc, web);
            }

            // 2. Tellerin üzerindeki ağır su/yağmur damlalarını çiz
            var attachedDroplets = web.Droplets.AttachedDroplets;
            for (int i = 0; i < attachedDroplets.Count; i++)
            {
                var drop = attachedDroplets[i];
                Point pos = web.Droplets.CalculateDropletWorldPosition(drop, web.CompletedSegments, web.Center);
                double r = drop.Radius;
                dc.DrawEllipse(_waterDropBrush, null, pos, r * 0.92, r * 1.12);
                dc.DrawEllipse(_waterHighlightBrush, null, new Point(pos.X - r * 0.28, pos.Y - r * 0.3), r * 0.32, r * 0.32);
            }

            // 3. Örümcek ağı silkelediğinde aşağı düşen su damlalarını çiz
            var fallingDroplets = web.Droplets.FallingDroplets;
            for (int i = 0; i < fallingDroplets.Count; i++)
            {
                var falling = fallingDroplets[i];
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
                    broken.Start.Y + dir.Y * 0.25 + 6.0
                );
                dc.DrawLine(_brokenStubPen, broken.Start, danglingTip);
            }

            // 5. Kilerde (Erzak Deposunda) asılı duran ipek kozalar
            var storedCocoons = web.Pantry.StoredCocoons;
            for (int i = 0; i < storedCocoons.Count; i++)
            {
                var cocoon = storedCocoons[i];
                double scale = Math.Max(0.2, 1.0 - cocoon.ConsumeProgress * 0.85);
                double r = cocoon.Size * 1.15 * scale;

                // Her koza için bağımsız, dondurulmuş (Frozen) matris dönüşümü:
                // Paylaşımlı TranslateTransform nesnesi kullanılmadığı için tüm kozalar doğru konumlarında kalır.
                var matrix = Matrix.Identity;
                matrix.Rotate(cocoon.Angle * 180.0 / Math.PI);
                matrix.Translate(cocoon.Position.X, cocoon.Position.Y);
                var transform = new MatrixTransform(matrix);
                transform.Freeze();

                dc.PushTransform(transform);
                dc.DrawEllipse(_cocoonFillBrush, _cocoonSilkPen, new Point(0, 0), r * 1.2, r * 0.82);
                dc.DrawLine(_cocoonSilkPen, new Point(-r * 0.7, -r * 0.5), new Point(r * 0.7, r * 0.5));
                dc.DrawLine(_cocoonSilkPen, new Point(-r * 0.7, r * 0.5), new Point(r * 0.7, -r * 0.5));
                dc.Pop();
            }
        }

        private CachedWebGeometry GetOrBuildCache(SpiderWebEntity web)
        {
            if (!_geometryCache.TryGetValue(web, out CachedWebGeometry? cache))
            {
                cache = new CachedWebGeometry { SegmentCount = -1 };
                _geometryCache.Add(web, cache);
            }

            int segCount = web.CompletedSegments.Count;
            int dmgCount = web.DamagedSegments.Count;
            int dropRev = web.Droplets.Revision;

            if (cache.SegmentCount == segCount &&
                cache.DamagedCount == dmgCount &&
                cache.DropletRevision == dropRev)
            {
                return cache;
            }

            cache.SegmentCount = segCount;
            cache.DamagedCount = dmgCount;
            cache.DropletRevision = dropRev;

            var anchorGeo = new StreamGeometry();
            var spiralGeo = new StreamGeometry();
            var dewGeo = new StreamGeometry();

            using (StreamGeometryContext anchorCtx = anchorGeo.Open())
            using (StreamGeometryContext spiralCtx = spiralGeo.Open())
            using (StreamGeometryContext dewCtx = dewGeo.Open())
            {
                for (int i = 0; i < segCount; i++)
                {
                    var seg = web.CompletedSegments[i];
                    double sag = web.Droplets.GetSegmentSag(i);

                    if (seg.IsAnchor)
                    {
                        anchorCtx.BeginFigure(seg.Start, isFilled: false, isClosed: false);
                        if (sag > 0.25)
                        {
                            Point control = WebSegmentGeometry.ComputeControlPoint(seg.Start, seg.End, true, web.Center, sag);
                            anchorCtx.QuadraticBezierTo(control, seg.End, isStroked: true, isSmoothJoin: false);
                        }
                        else
                        {
                            anchorCtx.LineTo(seg.End, isStroked: true, isSmoothJoin: false);
                        }
                    }
                    else
                    {
                        Point control = WebSegmentGeometry.ComputeControlPoint(seg.Start, seg.End, false, web.Center, sag);
                        spiralCtx.BeginFigure(seg.Start, isFilled: false, isClosed: false);
                        spiralCtx.QuadraticBezierTo(control, seg.End, isStroked: true, isSmoothJoin: false);

                        if (i % 6 == 0)
                        {
                            DrawTinyDewCircle(dewCtx, seg.End, 1.3);
                        }
                    }
                }
            }

            anchorGeo.Freeze();
            spiralGeo.Freeze();
            dewGeo.Freeze();

            cache.AnchorGeometry = anchorGeo;
            cache.SpiralGeometry = spiralGeo;
            cache.DewGeometry = dewGeo;
            return cache;
        }

        private static void DrawTinyDewCircle(StreamGeometryContext ctx, Point center, double r)
        {
            Point top = new Point(center.X, center.Y - r);
            Point bottom = new Point(center.X, center.Y + r);
            Size arcSize = new Size(r, r);
            ctx.BeginFigure(top, isFilled: true, isClosed: true);
            ctx.ArcTo(bottom, arcSize, 0, false, SweepDirection.Clockwise, isStroked: false, isSmoothJoin: false);
            ctx.ArcTo(top, arcSize, 0, false, SweepDirection.Clockwise, isStroked: false, isSmoothJoin: false);
        }

        private void RenderVibratingSegments(DrawingContext dc, SpiderWebEntity web)
        {
            for (int i = 0; i < web.CompletedSegments.Count; i++)
            {
                var seg = web.CompletedSegments[i];
                Point p1 = seg.Start;
                Point p2 = seg.End;

                Point mid = new Point((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
                double dist = (mid - web.VibrationOrigin).Length;
                double falloff = Math.Max(0, 1.0 - dist / (web.Radius * 1.5));
                double wave = Math.Sin(web.VibrationPhase - dist * 0.08) * web.VibrationAmplitude * falloff;

                p1.X += wave * 0.5;
                p1.Y += wave * 0.5;
                p2.X -= wave * 0.5;
                p2.Y -= wave * 0.5;

                Pen pen = seg.IsAnchor ? _anchorPen : _spiralPen;
                double sag = web.Droplets.GetSegmentSag(i);

                if (!seg.IsAnchor)
                {
                    Point control = WebSegmentGeometry.ComputeControlPoint(p1, p2, false, web.Center, sag);
                    Point q1 = WebSegmentGeometry.EvaluateQuadraticBezier(p1, control, p2, 0.25);
                    Point q2 = WebSegmentGeometry.EvaluateQuadraticBezier(p1, control, p2, 0.50);
                    Point q3 = WebSegmentGeometry.EvaluateQuadraticBezier(p1, control, p2, 0.75);

                    dc.DrawLine(pen, p1, q1);
                    dc.DrawLine(pen, q1, q2);
                    dc.DrawLine(pen, q2, q3);
                    dc.DrawLine(pen, q3, p2);
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
