using System.Windows.Media;

namespace SpiderDesk.Core
{
    public interface IRenderer<in T>
    {
        void Render(DrawingContext dc, T entity);
    }
}
