using System;
using System.Collections.Generic;
using System.Windows;

namespace SpiderDesk.Domain.Greeting
{
    /// <summary>Ekrana yerleştirilmiş bir yazının ipek çizgileri, kapladığı alan ve toplam çizgi uzunluğu.</summary>
    public sealed record SilkTextPath(
        IReadOnlyList<IReadOnlyList<Point>> Strokes,
        Rect Bounds,
        double LetterHeight,
        double TotalStrokeLength);

    /// <summary>
    /// Bir yazıyı harf tanımlarından (SilkGlyphs) ekran koordinatlarına yerleştiren saf hesaplama sınıfı.
    /// Yazı verilen merkeze ortalanır; verilen genişliğe sığmazsa harf yüksekliği küçültülür.
    /// </summary>
    public static class SilkTextLayout
    {
        public const double LetterGapRatio = 0.3;

        public static SilkTextPath Layout(string text, Point center, double maxWidth, double maxLetterHeight)
        {
            double unitWidth = MeasureUnitWidth(text);
            double letterHeight = unitWidth > 0 ? Math.Min(maxLetterHeight, maxWidth / unitWidth) : maxLetterHeight;

            double totalWidth = unitWidth * letterHeight;
            double left = center.X - totalWidth / 2.0;
            double top = center.Y - letterHeight / 2.0;

            var strokes = new List<IReadOnlyList<Point>>();
            double totalLength = 0;
            double cursorX = left;

            foreach (char character in text)
            {
                SilkGlyph glyph = SilkGlyphs.Get(character);
                foreach (var unitStroke in glyph.Strokes)
                {
                    var stroke = new Point[unitStroke.Count];
                    for (int i = 0; i < unitStroke.Count; i++)
                    {
                        stroke[i] = new Point(cursorX + unitStroke[i].X * letterHeight, top + unitStroke[i].Y * letterHeight);
                        if (i > 0) totalLength += (stroke[i] - stroke[i - 1]).Length;
                    }
                    strokes.Add(stroke);
                }
                cursorX += (glyph.Width + LetterGapRatio) * letterHeight;
            }

            return new SilkTextPath(strokes, new Rect(left, top, totalWidth, letterHeight), letterHeight, totalLength);
        }

        /// <summary>Yazının, harf yüksekliği 1 iken kapladığı genişlik (harf aralıkları dahil).</summary>
        public static double MeasureUnitWidth(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            double width = 0;
            foreach (char character in text)
            {
                width += SilkGlyphs.Get(character).Width;
            }
            return width + LetterGapRatio * (text.Length - 1);
        }
    }
}
