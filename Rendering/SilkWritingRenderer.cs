using System;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain.Greeting;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Örümceğin ipekle yazdığı selamı aktif temanın ağ rengiyle çizer.
    /// Teller çift iplik olarak çizilir; yeni gerilen tel bir an titrer, sonra yatay teller hafifçe sarkar.
    /// Titreme ve sarkma yalnızca görseldir, yazının konumlarını değiştirmez.
    /// </summary>
    public sealed class SilkWritingRenderer : IRenderer<SilkWriting>
    {
        private const double MainStrandThickness = 1.3;
        private const double SecondStrandThickness = 0.8;
        private const double SecondStrandOffset = 1.1;

        private const double WobbleRatio = 0.16;
        private const double WobbleDamping = 5.0;
        private const double WobbleFrequency = 16.0;
        private const double SagRatio = 0.06;

        private const double StreamLiftRatio = 0.08;
        private const double StreamWiggleFrequency = 9.0;
        private const double StreamWiggleRatio = 0.04;
        private const double StreamMaxWiggle = 10.0;
        private const int StreamParticleCount = 6;
        private const double StreamParticleSpeed = 1.8;

        private const double KnotRadius = 1.8;
        private const double TipRadius = 2.3;
        private const double ParticleRadius = 1.3;
        private const double BeadDrop = 0.6;
        private const double BeadFadeInRate = 1.5;
        private const double PuffSeconds = 0.35;
        private const double PuffGrowth = 9.0;
        private const double SnapFadeSeconds = 0.7;

        private readonly Func<ISpiderTheme> _themeProvider;
        private string _themeId = string.Empty;
        private Pen _mainStrandPen = null!;
        private Pen _secondStrandPen = null!;
        private Pen _streamPen = null!;
        private Pen _puffPen = null!;
        private SolidColorBrush _knotBrush = null!;
        private SolidColorBrush _beadBrush = null!;

        public SilkWritingRenderer(Func<ISpiderTheme> themeProvider)
        {
            _themeProvider = themeProvider;
        }

        public void Render(DrawingContext dc, SilkWriting writing)
        {
            if (writing.Opacity <= 0) return;

            EnsureTheme(_themeProvider());
            dc.PushOpacity(writing.Opacity);

            DrawSnappedStream(dc, writing);
            DrawThreads(dc, writing);
            DrawStream(dc, writing);
            DrawKnots(dc, writing);

            dc.Pop();
        }

        private void EnsureTheme(ISpiderTheme theme)
        {
            if (theme.Id == _themeId) return;
            _themeId = theme.Id;

            Color silk = theme.WebAnchorColor;
            _mainStrandPen = DrawingResources.CreateFrozenPen(WithAlpha(silk, 230), MainStrandThickness);
            _secondStrandPen = DrawingResources.CreateFrozenPen(WithAlpha(silk, 105), SecondStrandThickness);
            _streamPen = DrawingResources.CreateFrozenPen(WithAlpha(silk, 165), 1.0);
            _puffPen = DrawingResources.CreateFrozenPen(WithAlpha(silk, 205), 1.0);
            _knotBrush = DrawingResources.CreateFrozenBrush(WithAlpha(silk, 245));
            _beadBrush = DrawingResources.CreateFrozenBrush(WithAlpha(silk, 215));
        }

        private void DrawThreads(DrawingContext dc, SilkWriting writing)
        {
            var threads = writing.Threads;
            if (threads.Count == 0) return;

            var mainStrands = new StreamGeometry();
            var secondStrands = new StreamGeometry();

            using (StreamGeometryContext main = mainStrands.Open())
            using (StreamGeometryContext second = secondStrands.Open())
            {
                for (int i = 0; i < threads.Count; i++)
                {
                    SilkThread thread = threads[i];
                    Point control = ThreadControlPoint(thread, writing.Age);
                    Vector offset = Perpendicular(thread.Start, thread.End) * SecondStrandOffset;

                    main.BeginFigure(thread.Start, isFilled: false, isClosed: false);
                    main.QuadraticBezierTo(control, thread.End, isStroked: true, isSmoothJoin: false);

                    second.BeginFigure(thread.Start + offset, isFilled: false, isClosed: false);
                    second.QuadraticBezierTo(control + offset * 1.8, thread.End + offset, isStroked: true, isSmoothJoin: false);
                }
            }

            mainStrands.Freeze();
            secondStrands.Freeze();
            dc.DrawGeometry(null, _mainStrandPen, mainStrands);
            dc.DrawGeometry(null, _secondStrandPen, secondStrands);

            for (int i = 0; i < threads.Count; i++)
            {
                DrawBeads(dc, threads[i], writing.Age);
            }
        }

        private void DrawBeads(DrawingContext dc, SilkThread thread, double age)
        {
            if (thread.Beads.Count == 0) return;

            double fadeIn = Math.Min(1.0, (age - thread.LaidAt) * BeadFadeInRate);
            if (fadeIn <= 0) return;

            Point control = ThreadControlPoint(thread, age);
            dc.PushOpacity(fadeIn);
            foreach (var bead in thread.Beads)
            {
                Point p = QuadraticPoint(thread.Start, control, thread.End, bead.Position);
                dc.DrawEllipse(_beadBrush, null, new Point(p.X, p.Y + BeadDrop), bead.Radius, bead.Radius);
            }
            dc.Pop();
        }

        /// <summary>Örümcekten yazının ucuna uzanan kesintisiz ip, içinde akan ipek tanecikleri ve serilmekte olan çizgi.</summary>
        private void DrawStream(DrawingContext dc, SilkWriting writing)
        {
            if (writing.StreamOrigin is not { } origin || writing.StreamTip is not { } tip) return;

            double distance = (tip - origin).Length;
            double wiggle = writing.IsStreamFlowing
                ? Math.Sin(writing.Age * StreamWiggleFrequency) * Math.Min(StreamMaxWiggle, distance * StreamWiggleRatio)
                : 0.0;
            Point control = StreamControlPoint(origin, tip, wiggle);

            dc.DrawGeometry(null, _streamPen, QuadraticGeometry(origin, control, tip));

            if (writing.IsStreamFlowing)
            {
                for (int i = 0; i < StreamParticleCount; i++)
                {
                    double t = (writing.Age * StreamParticleSpeed + (double)i / StreamParticleCount) % 1.0;
                    dc.DrawEllipse(_knotBrush, null, QuadraticPoint(origin, control, tip, t), ParticleRadius, ParticleRadius);
                }
            }

            if (writing.StrokeInProgressStart is { } strokeStart)
            {
                dc.DrawLine(_mainStrandPen, strokeStart, tip);
            }

            dc.DrawEllipse(_knotBrush, null, tip, TipRadius, TipRadius);
        }

        private void DrawSnappedStream(DrawingContext dc, SilkWriting writing)
        {
            if (writing.Snap is not { } snap) return;

            double remaining = 1.0 - (writing.Age - snap.SnappedAt) / SnapFadeSeconds;
            if (remaining <= 0) return;

            dc.PushOpacity(remaining);
            dc.DrawGeometry(null, _streamPen, QuadraticGeometry(snap.From, StreamControlPoint(snap.From, snap.To, 0.0), snap.To));
            dc.Pop();
        }

        /// <summary>Düğümler ve fırlatılan ipin ilk değdiği yerlerde kısa süre genişleyen yapışma halkası.</summary>
        private void DrawKnots(DrawingContext dc, SilkWriting writing)
        {
            foreach (var knot in writing.Knots)
            {
                dc.DrawEllipse(_knotBrush, null, knot.Position, KnotRadius, KnotRadius);

                if (!knot.IsLaunchPoint) continue;
                double puff = (writing.Age - knot.TiedAt) / PuffSeconds;
                if (puff >= 1.0) continue;

                double radius = 2.0 + puff * PuffGrowth;
                dc.PushOpacity(1.0 - puff);
                dc.DrawEllipse(null, _puffPen, knot.Position, radius, radius);
                dc.Pop();
            }
        }

        /// <summary>Tel gerildikten hemen sonra sönümlenen titreşim ve yatay tellerde yerçekimiyle oluşan hafif sarkma.</summary>
        private static Point ThreadControlPoint(SilkThread thread, double age)
        {
            double timeSinceLaid = Math.Max(0, age - thread.LaidAt);
            Vector perpendicular = Perpendicular(thread.Start, thread.End);
            double wobble = thread.Length * WobbleRatio * Math.Exp(-WobbleDamping * timeSinceLaid) * Math.Cos(WobbleFrequency * timeSinceLaid);
            double horizontalShare = thread.Length > 0 ? Math.Abs(thread.End.X - thread.Start.X) / thread.Length : 0;
            double sag = thread.Length * SagRatio * horizontalShare;

            return new Point(
                (thread.Start.X + thread.End.X) / 2.0 + perpendicular.X * wobble,
                (thread.Start.Y + thread.End.Y) / 2.0 + perpendicular.Y * wobble + sag);
        }

        private static Point StreamControlPoint(Point origin, Point tip, double wiggle)
        {
            double distance = (tip - origin).Length;
            Vector perpendicular = Perpendicular(origin, tip);
            return new Point(
                (origin.X + tip.X) / 2.0 + perpendicular.X * wiggle,
                (origin.Y + tip.Y) / 2.0 + perpendicular.Y * wiggle - distance * StreamLiftRatio);
        }

        private static Vector Perpendicular(Point start, Point end)
        {
            Vector direction = end - start;
            double length = direction.Length;
            return length > 0.0001 ? new Vector(-direction.Y / length, direction.X / length) : new Vector(0, 0);
        }

        private static StreamGeometry QuadraticGeometry(Point start, Point control, Point end)
        {
            var geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(start, isFilled: false, isClosed: false);
                ctx.QuadraticBezierTo(control, end, isStroked: true, isSmoothJoin: false);
            }
            geometry.Freeze();
            return geometry;
        }

        private static Point QuadraticPoint(Point start, Point control, Point end, double t) =>
            WebSegmentGeometry.EvaluateQuadraticBezier(start, control, end, t);

        private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
