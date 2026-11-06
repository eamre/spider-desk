using System;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Bir ağın ipek tellerini kalem başına tek bir dondurulmuş geometri olarak tutar.
    /// Binlerce ayrı çizim komutu yerine üç çizim çağrısıyla çizilebilir.
    /// </summary>
    internal sealed record WebSilkGeometry(StreamGeometry Anchors, StreamGeometry Spirals, StreamGeometry Dew);

    /// <summary>
    /// Ağ verisini ve bir <see cref="WebDeformation"/>'ı dondurulmuş WPF geometrilerine çeviren saf üretici.
    /// Durgun ağ (önbelleğe alınır) ile sallanan ağ (her karede üretilir) aynı kodu kullanır; böylece
    /// hareket sırasındaki görüntü durgun görüntüyle birebir aynı kalır.
    /// </summary>
    internal static class WebGeometryBuilder
    {
        private const int DewEveryNthSegment = 6;
        private const double DewRadius = 1.3;
        private const double AnchorSagCurveThreshold = 0.25;

        private const double CocoonBaseScale = 1.15;
        private const double CocoonMinScale = 0.2;
        private const double CocoonRadiusX = 1.2;
        private const double CocoonRadiusY = 0.82;
        private const double CocoonWrapHalfWidth = 0.7;
        private const double CocoonWrapHalfHeight = 0.5;
        private const double CocoonBreezeTilt = 0.02;

        public static WebSilkGeometry BuildSilk(SpiderWebEntity web, in WebDeformation deformation)
        {
            var anchors = new StreamGeometry();
            var spirals = new StreamGeometry();
            var dew = new StreamGeometry();

            using (StreamGeometryContext anchorCtx = anchors.Open())
            using (StreamGeometryContext spiralCtx = spirals.Open())
            using (StreamGeometryContext dewCtx = dew.Open())
            {
                Point curveCenter = deformation.CurveCenter;
                var segments = web.CompletedSegments;

                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    Point start = segment.Start;
                    Point end = segment.End;
                    deformation.DeformSegment(ref start, ref end);

                    double sag = web.Droplets.GetSegmentSag(i);

                    if (segment.IsAnchor)
                    {
                        AppendAnchor(anchorCtx, start, end, curveCenter, sag);
                    }
                    else
                    {
                        AppendSpiral(spiralCtx, start, end, curveCenter, sag);
                        if (i % DewEveryNthSegment == 0)
                        {
                            AppendCircle(dewCtx, end, DewRadius);
                        }
                    }
                }
            }

            anchors.Freeze();
            spirals.Freeze();
            dew.Freeze();
            return new WebSilkGeometry(anchors, spirals, dew);
        }

        /// <summary>
        /// Kilerdeki tüm kozaları (dolgulu elips gövde + çapraz sargı ipliği) tek bir geometride toplar.
        /// Koza başına dönüşüm nesnesi (MatrixTransform) üretmek yerine noktalar doğrudan dünya koordinatında hesaplanır.
        /// </summary>
        public static StreamGeometry BuildCocoons(SpiderWebEntity web, in WebDeformation deformation)
        {
            var geometry = new StreamGeometry();
            var cocoons = web.Pantry.StoredCocoons;

            using (StreamGeometryContext ctx = geometry.Open())
            {
                for (int i = 0; i < cocoons.Count; i++)
                {
                    var cocoon = cocoons[i];
                    double breeze = deformation.BreezeInfluence(cocoon.Position);
                    Point center = deformation.Displace(cocoon.Position);
                    double angle = cocoon.Angle + breeze * CocoonBreezeTilt;

                    double scale = Math.Max(CocoonMinScale, 1.0 - cocoon.ConsumeProgress * SpiderTuning.CocoonConsumedScaleReduction);
                    double r = cocoon.Size * CocoonBaseScale * scale;

                    AppendCocoon(ctx, center, angle, r);
                }
            }

            geometry.Freeze();
            return geometry;
        }

        private static void AppendAnchor(StreamGeometryContext ctx, Point start, Point end, Point curveCenter, double sag)
        {
            ctx.BeginFigure(start, isFilled: false, isClosed: false);
            if (sag > AnchorSagCurveThreshold)
            {
                Point control = WebSegmentGeometry.ComputeControlPoint(start, end, true, curveCenter, sag);
                ctx.QuadraticBezierTo(control, end, isStroked: true, isSmoothJoin: false);
            }
            else
            {
                ctx.LineTo(end, isStroked: true, isSmoothJoin: false);
            }
        }

        private static void AppendSpiral(StreamGeometryContext ctx, Point start, Point end, Point curveCenter, double sag)
        {
            Point control = WebSegmentGeometry.ComputeControlPoint(start, end, false, curveCenter, sag);
            ctx.BeginFigure(start, isFilled: false, isClosed: false);
            ctx.QuadraticBezierTo(control, end, isStroked: true, isSmoothJoin: false);
        }

        private static void AppendCircle(StreamGeometryContext ctx, Point center, double radius)
        {
            AppendEllipse(ctx, center, radius, radius, rotationRadians: 0.0);
        }

        private static void AppendCocoon(StreamGeometryContext ctx, Point center, double angle, double r)
        {
            AppendEllipse(ctx, center, r * CocoonRadiusX, r * CocoonRadiusY, angle);

            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            double w = r * CocoonWrapHalfWidth;
            double h = r * CocoonWrapHalfHeight;

            AppendLine(ctx, Rotate(center, -w, -h, cos, sin), Rotate(center, w, h, cos, sin));
            AppendLine(ctx, Rotate(center, -w, h, cos, sin), Rotate(center, w, -h, cos, sin));
        }

        /// <summary>Dönük bir elipsi iki yarım yay olarak ekler (dolgulu figür).</summary>
        private static void AppendEllipse(StreamGeometryContext ctx, Point center, double radiusX, double radiusY, double rotationRadians)
        {
            double cos = Math.Cos(rotationRadians);
            double sin = Math.Sin(rotationRadians);
            Point right = Rotate(center, radiusX, 0.0, cos, sin);
            Point left = Rotate(center, -radiusX, 0.0, cos, sin);
            var size = new Size(radiusX, radiusY);
            double rotationDegrees = rotationRadians * 180.0 / Math.PI;

            ctx.BeginFigure(right, isFilled: true, isClosed: true);
            ctx.ArcTo(left, size, rotationDegrees, false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
            ctx.ArcTo(right, size, rotationDegrees, false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
        }

        private static void AppendLine(StreamGeometryContext ctx, Point from, Point to)
        {
            ctx.BeginFigure(from, isFilled: false, isClosed: false);
            ctx.LineTo(to, isStroked: true, isSmoothJoin: false);
        }

        /// <summary>Yerel (x, y) noktasını açıya göre döndürüp merkeze taşır (WPF Matrix.Rotate + Translate ile aynı yön).</summary>
        private static Point Rotate(Point center, double x, double y, double cos, double sin) =>
            new(center.X + x * cos - y * sin, center.Y + x * sin + y * cos);
    }
}
