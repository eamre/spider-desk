using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Yalnızca örümcek ağlarını, çiy damlalarını ve kilerdeki kozaları çizen katman.
    /// Ağlarda bir değişiklik veya titreşim olmadıkça yeniden boyanmaz; WPF görsel ağacında önbellekli tutulur.
    /// </summary>
    public class WebLayerCanvas : FrameworkElement
    {
        private readonly WorldContext _world;
        private readonly IRenderer<SpiderWebEntity> _webRenderer;
        private int _lastWebStateHash = -1;

        public WebLayerCanvas(WorldContext world, IRenderer<SpiderWebEntity> webRenderer)
        {
            IsHitTestVisible = false;
            _world = world;
            _webRenderer = webRenderer;
        }

        public void RefreshIfNeeded(bool force = false)
        {
            int currentHash = ComputeWebStateHash();
            if (force || HasActiveWebAnimation() || currentHash != _lastWebStateHash)
            {
                _lastWebStateHash = currentHash;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            for (int i = 0; i < _world.Webs.Count; i++)
            {
                _webRenderer.Render(dc, _world.Webs[i]);
            }
        }

        private bool HasActiveWebAnimation()
        {
            for (int i = 0; i < _world.Webs.Count; i++)
            {
                var web = _world.Webs[i];
                if (web.VibrationAmplitude > 0.05 || web.Droplets.FallingDroplets.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private int ComputeWebStateHash()
        {
            int hash = _world.Webs.Count * 397;
            for (int i = 0; i < _world.Webs.Count; i++)
            {
                var web = _world.Webs[i];
                hash = (hash * 31)
                       ^ web.CompletedSegments.Count
                       ^ (web.DamagedSegments.Count << 8)
                       ^ (web.Droplets.Revision << 16)
                       ^ (web.Pantry.StoredCocoons.Count << 24);

                var cocoons = web.Pantry.StoredCocoons;
                for (int c = 0; c < cocoons.Count; c++)
                {
                    hash = (hash * 17) ^ (int)(cocoons[c].ConsumeProgress * 100);
                }
            }
            return hash;
        }
    }
}
