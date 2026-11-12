using System;
using System.Collections.Generic;
using System.Windows;

namespace SpiderDesk.Domain.Greeting
{
    /// <summary>
    /// Tek bir harfin genişliği ve ipekle çizilecek çizgileri. Her çizgi, ipin ucunun kesintisiz izlediği nokta listesidir.
    /// Koordinatlar yüksekliği 1 olan birim kutudadır: x sağa, y aşağı doğru artar.
    /// </summary>
    public sealed record SilkGlyph(double Width, IReadOnlyList<IReadOnlyList<Point>> Strokes);

    /// <summary>
    /// Selam yazılarında kullanılan büyük harflerin tek çizgili tanımları.
    /// Tanımlı olmayan karakterler boşluk olarak yazılır.
    /// </summary>
    public static class SilkGlyphs
    {
        private const int ArcSegments = 18;
        private const int RingSegments = 36;

        private static readonly SilkGlyph Space = new(0.45, Array.Empty<IReadOnlyList<Point>>());
        private static readonly IReadOnlyDictionary<char, SilkGlyph> Glyphs = BuildGlyphs();

        public static SilkGlyph Get(char character) =>
            Glyphs.TryGetValue(char.ToUpperInvariant(character), out SilkGlyph? glyph) ? glyph : Space;

        private static Dictionary<char, SilkGlyph> BuildGlyphs() => new()
        {
            ['D'] = new(0.62, new[]
            {
                Stroke(P(0, 0), P(0, 1)),
                Stroke(Join(new[] { P(0, 0), P(0.3, 0) }, Arc(0.3, 0.5, 0.32, 0.5, -90, 90, ArcSegments), new[] { P(0, 1) }))
            }),
            ['E'] = new(0.62, new[]
            {
                Stroke(P(0.62, 0), P(0, 0), P(0, 1), P(0.62, 1)),
                Stroke(P(0, 0.5), P(0.5, 0.5))
            }),
            ['G'] = new(0.8, new[]
            {
                Stroke(Join(Arc(0.4, 0.5, 0.4, 0.5, -40, -360, 32), new[] { P(0.45, 0.5) }))
            }),
            ['H'] = new(0.72, new[]
            {
                Stroke(P(0, 0), P(0, 1)),
                Stroke(P(0.72, 0), P(0.72, 1)),
                Stroke(P(0, 0.5), P(0.72, 0.5))
            }),
            ['I'] = new(0.04, new[]
            {
                Stroke(P(0.02, 0), P(0.02, 1))
            }),
            ['L'] = new(0.55, new[]
            {
                Stroke(P(0, 0), P(0, 1), P(0.55, 1))
            }),
            ['M'] = new(0.8, new[]
            {
                Stroke(P(0, 1), P(0, 0), P(0.4, 0.6), P(0.8, 0), P(0.8, 1))
            }),
            ['N'] = new(0.68, new[]
            {
                Stroke(P(0, 1), P(0, 0), P(0.68, 1), P(0.68, 0))
            }),
            ['O'] = new(0.8, new[]
            {
                Stroke(Arc(0.4, 0.5, 0.4, 0.5, -90, -450, RingSegments))
            }),
            ['R'] = new(0.62, new[]
            {
                Stroke(Join(new[] { P(0, 1), P(0, 0), P(0.32, 0) }, Arc(0.32, 0.25, 0.26, 0.25, -90, 90, ArcSegments), new[] { P(0, 0.5) })),
                Stroke(P(0.26, 0.5), P(0.62, 1))
            }),
            ['T'] = new(0.7, new[]
            {
                Stroke(P(0, 0), P(0.7, 0)),
                Stroke(P(0.35, 0), P(0.35, 1))
            }),
            ['V'] = new(0.7, new[]
            {
                Stroke(P(0, 0), P(0.35, 1), P(0.7, 0))
            })
        };

        private static Point P(double x, double y) => new(x, y);

        /// <summary>Saat yönünde artan (veya azalan) açılarla elips yayı üretir; uç noktalar dahildir.</summary>
        private static Point[] Arc(double cx, double cy, double rx, double ry, double fromDegrees, double toDegrees, int segments)
        {
            var points = new Point[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                double angle = (fromDegrees + (toDegrees - fromDegrees) * i / segments) * Math.PI / 180.0;
                points[i] = new Point(cx + rx * Math.Cos(angle), cy + ry * Math.Sin(angle));
            }
            return points;
        }

        private static Point[] Join(params Point[][] parts)
        {
            var joined = new List<Point>();
            foreach (var part in parts) joined.AddRange(part);
            return joined.ToArray();
        }

        /// <summary>Ardışık çakışan noktaları atar; böylece sıfır uzunluklu ipek teli oluşmaz.</summary>
        private static IReadOnlyList<Point> Stroke(params Point[] points)
        {
            var stroke = new List<Point>(points.Length);
            foreach (var point in points)
            {
                if (stroke.Count == 0 || (point - stroke[^1]).LengthSquared > 1e-9)
                {
                    stroke.Add(point);
                }
            }
            return stroke;
        }
    }
}
