using System.Windows.Media;

namespace SpiderDesk.Rendering
{
    public interface IRenderer<in T>
    {
        void Render(DrawingContext dc, T entity);
    }
}
