using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain.Insects;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Sadece böcekleri, ateş böceği parıltısını (bioluminescence) ve ipek kozayı çizmekten sorumludur (SRP).
    /// Fırçalar önbelleğe alınarak her karede bellek tahsisi (GC baskısı) önlenir.
    /// </summary>
    public class BugRenderer : IRenderer<BugEntity>
    {
        private readonly SolidColorBrush _wingBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(145, 220, 240, 255));
        private readonly Pen _silkPen = DrawingResources.CreateFrozenPen(Color.FromArgb(220, 255, 255, 255), 1.2);
        private readonly Dictionary<Color, SolidColorBrush> _brushCache = new();

        private readonly TranslateTransform _bugTranslate = new();
        private readonly RotateTransform _bugRotate = new();
        private readonly RotateTransform _leftWingRotate = new();
        private readonly RotateTransform _rightWingRotate = new();

        public void Render(DrawingContext dc, BugEntity bug)
        {
            _bugTranslate.X = bug.Position.X;
            _bugTranslate.Y = bug.Position.Y;
            _bugRotate.Angle = bug.Angle * 180.0 / Math.PI;

            dc.PushTransform(_bugTranslate);
            dc.PushTransform(_bugRotate);

            // 1. Ateş Böceği (Firefly) Işık Halesi (Bioluminescence)
            if (bug.Species.GlowColor is { } glowColor)
            {
                double pulse = (Math.Sin(bug.GlowPhase) + 1.0) * 0.5; // 0.0 .. 1.0
                byte outerAlpha = (byte)(35 + pulse * 75);
                byte innerAlpha = (byte)(120 + pulse * 125);

                var outerGlow = GetCachedBrush(Color.FromArgb(outerAlpha, glowColor.R, glowColor.G, glowColor.B));
                var innerGlow = GetCachedBrush(Color.FromArgb(innerAlpha, glowColor.R, glowColor.G, glowColor.B));

                double glowRadius = bug.Size * (1.7 + pulse * 0.9);
                dc.DrawEllipse(outerGlow, null, new Point(-bug.Size * 0.4, 0), glowRadius, glowRadius * 0.85);
                dc.DrawEllipse(innerGlow, null, new Point(-bug.Size * 0.45, 0), bug.Size * 0.55, bug.Size * 0.45);
            }

            // 2. Kanatlar (Konmuş böceklerde WingPhase = 0 olduğu için kanatlar kapalı durur)
            double wingSpread = Math.Sin(bug.WingPhase) * 32.0;
            double baseWingAngle = Math.Abs(bug.WingPhase) < 0.001 ? 14.0 : 35.0;

            _leftWingRotate.Angle = -baseWingAngle - wingSpread;
            dc.PushTransform(_leftWingRotate);
            dc.DrawEllipse(_wingBrush, null, new Point(-1, -bug.Size * 0.75), bug.Size * 0.9, bug.Size * 0.46);
            dc.Pop();

            _rightWingRotate.Angle = baseWingAngle + wingSpread;
            dc.PushTransform(_rightWingRotate);
            dc.DrawEllipse(_wingBrush, null, new Point(-1, bug.Size * 0.75), bug.Size * 0.9, bug.Size * 0.46);
            dc.Pop();

            // 3. Böcek Gövdesi ve Baş
            var bodyBrush = GetCachedBrush(bug.Species.BodyColor);
            dc.DrawEllipse(bodyBrush, null, new Point(0, 0), bug.Size, bug.Size * 0.62);
            dc.DrawEllipse(Brushes.Black, null, new Point(bug.Size * 0.75, 0), bug.Size * 0.42, bug.Size * 0.42);

            // 4. İpek Koza (Örümcek sararken)
            if (bug.WrapProgress > 0.01)
            {
                byte alpha = (byte)Math.Min(245, 70 + bug.WrapProgress * 175);
                // Alfa değeri10'luk adımlarla yuvarlanarak önbellekten kullanılır
                byte quantizedAlpha = (byte)((alpha / 10) * 10);
                var cocoonBrush = GetCachedBrush(Color.FromArgb(quantizedAlpha, 245, 250, 255));
                double r = bug.Size * (0.6 + 0.65 * bug.WrapProgress);
                dc.DrawEllipse(cocoonBrush, _silkPen, new Point(0, 0), r * 1.2, r * 0.85);
            }

            dc.Pop();
            dc.Pop();
        }

        private SolidColorBrush GetCachedBrush(Color color)
        {
            if (!_brushCache.TryGetValue(color, out var brush))
            {
                brush = DrawingResources.CreateFrozenBrush(color);
                _brushCache[color] = brush;
            }
            return brush;
        }
    }
}
