using System.Windows.Media;

namespace SpiderDesk.Core
{
    /// <summary>
    /// Örümcek ve ağlarının görsel renk paletini tanımlayan arayüz.
    /// </summary>
    public interface ISpiderTheme
    {
        string Id { get; }
        string DisplayName { get; }
        Color LegUpperColor { get; }
        Color LegLowerColor { get; }
        Color JointColor { get; }
        Color AbdomenColor { get; }
        Color ThoraxColor { get; }
        Color MarkingColor { get; }
        Color EyeColor { get; }
        Color WebAnchorColor { get; }
        Color WebSpiralColor { get; }
    }
}
