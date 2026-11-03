using System.Windows.Media;

namespace SpiderDesk.Core
{
    /// <summary>
    /// Böcek türlerinin (Karasinek, Ateş Böceği, Yaban Arısı vb.) özelliklerini tanımlayan arayüz (OCP & ISP).
    /// Yeni bir böcek türü eklemek için bu arayüzü uygulamak yeterlidir.
    /// </summary>
    public interface IBugSpecies
    {
        string Name { get; }
        Color BodyColor { get; }
        Color? GlowColor { get; }
        double BaseSize { get; }
        double MinSpeed { get; }
        double MaxSpeed { get; }
        int WebDamageOnImpact { get; }
        double EscapeChance { get; }
        bool CanPerchOnDesktop { get; }
    }
}
