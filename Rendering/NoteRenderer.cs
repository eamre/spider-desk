using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Örümceğin ön bacaklarıyla tuttuğu kâğıt notu çizer: el yazısı gibi bir yazı tipiyle, hafif gölgeli krem bir kart.
    /// Kartın genişliği metne göre ayarlanır, uzun metin alt satıra geçer.
    /// </summary>
    public sealed class NoteRenderer : IRenderer<CarriedNote>
    {
        private const double FontSize = 15.0;
        private const double MaxTextWidth = 190.0;
        private const double MinCardWidth = 96.0;
        private const double PaddingX = 14.0;
        private const double PaddingY = 10.0;
        private const double CornerRadius = 3.0;
        private static readonly Vector ShadowOffset = new(2.0, 3.0);

        private static readonly Typeface HandwritingTypeface = new(
            new FontFamily("Segoe Print, Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        private readonly SolidColorBrush _paperBrush = DrawingResources.CreateFrozenBrush(Color.FromRgb(255, 251, 236));
        private readonly SolidColorBrush _shadowBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(55, 0, 0, 0));
        private readonly SolidColorBrush _inkBrush = DrawingResources.CreateFrozenBrush(Color.FromRgb(62, 50, 40));
        private readonly Pen _edgePen = DrawingResources.CreateFrozenPen(Color.FromRgb(214, 202, 170), 1.0);

        // Metin her karede değişmediği için ölçülmüş yazı saklanır; yalnızca metin değişince yeniden ölçülür
        private string? _cachedText;
        private FormattedText? _cachedFormattedText;

        public void Render(DrawingContext dc, CarriedNote note)
        {
            if (note.Opacity <= 0) return;

            FormattedText text = GetFormattedText(note.Text);
            double cardWidth = Math.Max(MinCardWidth, text.Width + PaddingX * 2);
            double cardHeight = text.Height + PaddingY * 2;
            var card = new Rect(note.TopCenter.X - cardWidth / 2, note.TopCenter.Y, cardWidth, cardHeight);

            dc.PushOpacity(note.Opacity);
            dc.PushTransform(new RotateTransform(note.Tilt * 180.0 / Math.PI, note.TopCenter.X, note.TopCenter.Y));
            dc.DrawRoundedRectangle(_shadowBrush, null, Rect.Offset(card, ShadowOffset), CornerRadius, CornerRadius);
            dc.DrawRoundedRectangle(_paperBrush, _edgePen, card, CornerRadius, CornerRadius);
            // Ortalı hizalamada FormattedText satırları MaxTextWidth genişliğindeki kutunun içinde kendisi ortalar
            dc.DrawText(text, new Point(note.TopCenter.X - MaxTextWidth / 2, card.Y + PaddingY));
            dc.Pop();
            dc.Pop();
        }

        private FormattedText GetFormattedText(string text)
        {
            if (_cachedFormattedText != null && _cachedText == text)
            {
                return _cachedFormattedText;
            }

            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                HandwritingTypeface,
                FontSize,
                _inkBrush,
                PixelsPerDip())
            {
                MaxTextWidth = MaxTextWidth,
                TextAlignment = TextAlignment.Center
            };

            _cachedText = text;
            _cachedFormattedText = formatted;
            return formatted;
        }

        private static double PixelsPerDip() =>
            Application.Current?.MainWindow is { } window ? VisualTreeHelper.GetDpi(window).PixelsPerDip : 1.0;
    }
}
