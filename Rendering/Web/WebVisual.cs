using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering.Web
{
    /// <summary>
    /// Tek bir örümcek ağının görsel düğümü. Yalnızca kendi ağı değiştiğinde veya hareket ettiğinde yeniden çizilir;
    /// böylece bir ağ rüzgarda sallanırken diğer ağlar WPF tarafından yeniden rasterize edilmez.
    /// </summary>
    internal sealed class WebVisual : DrawingVisual
    {
        private readonly IRenderer<SpiderWebEntity> _renderer;
        private bool _hasRendered;
        private bool _wasAnimating;
        private int _lastStateHash;

        public SpiderWebEntity Web { get; }

        public WebVisual(SpiderWebEntity web, IRenderer<SpiderWebEntity> renderer)
        {
            Web = web;
            _renderer = renderer;
        }

        public void RefreshIfNeeded(bool force)
        {
            bool isAnimating = Web.IsAnimating;
            int stateHash = Web.ComputeVisualStateHash();

            // Hareketin bittiği ilk karede de bir kez çizilir: ağ son kaydığı konumda kalmaz, durgun haline oturur.
            bool needsRender = force
                || !_hasRendered
                || isAnimating
                || _wasAnimating
                || stateHash != _lastStateHash;

            _wasAnimating = isAnimating;
            _lastStateHash = stateHash;

            if (!needsRender) return;

            using (DrawingContext dc = RenderOpen())
            {
                _renderer.Render(dc, Web);
            }

            _hasRendered = true;
        }
    }
}
