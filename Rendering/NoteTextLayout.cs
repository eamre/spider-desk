using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Örümceğin tuttuğu notun yazısını satırlara dizer: el yazısı gibi bir yazı tipi, ortalı satırlar.
    /// Satıra sığmayan uzun bir kelime (boşluksuz yazılmış bir cümle ya da bir internet adresi) kesilmez,
    /// sığmadığı yerden alt satıra devam eder. Böylece notun yazısı hiçbir zaman "..." ile kısalmaz.
    /// </summary>
    public static class NoteTextLayout
    {
        public const double FontSize = 15.0;

        /// <summary>Yazının sığdığı en geniş satır; kart buna göre genişler.</summary>
        public const double MaxTextWidth = 190.0;

        // Görünmez "buradan bölünebilir" işareti: ekranda yer kaplamaz, yalnızca satır sonu için izin verir
        private const char BreakOpportunity = '​';

        private static readonly Typeface HandwritingTypeface = new(
            new FontFamily("Segoe Print, Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        public static FormattedText Create(string text, Brush ink, double pixelsPerDip)
        {
            var formatted = Format(WithBreakableLongWords(text, pixelsPerDip), ink, pixelsPerDip);
            formatted.MaxTextWidth = MaxTextWidth;
            formatted.TextAlignment = TextAlignment.Center;
            // Hiçbir şey "..." ile kısaltılmasın: bütün yazı görünmeli
            formatted.Trimming = TextTrimming.None;
            return formatted;
        }

        /// <summary>Satıra sığmayan her kelimenin harflerinin arasına görünmez bölme işareti koyar; sığanlara dokunmaz.</summary>
        private static string WithBreakableLongWords(string text, double pixelsPerDip)
        {
            var result = new StringBuilder(text.Length);
            foreach (string word in text.Split(' '))
            {
                if (result.Length > 0) result.Append(' ');

                if (Format(word, Brushes.Black, pixelsPerDip).Width <= MaxTextWidth)
                {
                    result.Append(word);
                    continue;
                }

                for (int i = 0; i < word.Length; i++)
                {
                    if (i > 0) result.Append(BreakOpportunity);
                    result.Append(word[i]);
                }
            }
            return result.ToString();
        }

        private static FormattedText Format(string text, Brush ink, double pixelsPerDip) =>
            new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, HandwritingTypeface, FontSize, ink, pixelsPerDip);
    }
}
