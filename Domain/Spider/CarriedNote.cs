using System.Windows;

namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Örümceğin ön bacaklarıyla tabela gibi tuttuğu kâğıt not (örneğin bir hatırlatma).
    /// Kartın boyutunu çizim katmanı metne göre belirler; simülasyon yalnızca kartın üst kenarının ortasını bilir.
    /// </summary>
    public sealed class CarriedNote
    {
        public string Text { get; }

        /// <summary>Kartın üst kenarının ortası. Kart buradan aşağı doğru sarkar.</summary>
        public Point TopCenter { get; set; }

        /// <summary>Kartın eğikliği (radyan, 0 = dik). Örümcek sallanıp eğildikçe tuttuğu kart da onunla eğilir.</summary>
        public double Tilt { get; set; }

        /// <summary>0 = görünmez, 1 = tam görünür. Kart belirerek gelir, solarak gider.</summary>
        public double Opacity { get; set; }

        public CarriedNote(string text)
        {
            Text = text;
        }
    }
}
