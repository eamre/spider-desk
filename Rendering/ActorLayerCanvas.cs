using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Yalnızca hareketli varlıkları (örümcek ve uçan/yakalanan böcekler) çizen katman.
    /// Ağ katmanından bağımsız güncellenerek her karede tüm ağların yeniden taranmasını önler.
    /// </summary>
    public class ActorLayerCanvas : FrameworkElement
    {
        private readonly WorldContext _world;
        private readonly SpiderEntity _spider;
        private readonly IRenderer<BugEntity> _bugRenderer;
        private readonly IRenderer<SpiderEntity> _spiderRenderer;
        private bool _wasIdle;

        public ActorLayerCanvas(
            WorldContext world,
            SpiderEntity spider,
            IRenderer<BugEntity> bugRenderer,
            IRenderer<SpiderEntity> spiderRenderer)
        {
            IsHitTestVisible = false;
            _world = world;
            _spider = spider;
            _bugRenderer = bugRenderer;
            _spiderRenderer = spiderRenderer;
        }

        public void RefreshIfNeeded(bool force = false)
        {
            bool hasActorMotion = _spider.HasVisualMotion
                || _world.Bugs.Count > 0
                || _spider.ActiveWeb?.IsBreezing == true;
            if (force || hasActorMotion || !_wasIdle)
            {
                _wasIdle = !hasActorMotion;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            for (int i = 0; i < _world.Bugs.Count; i++)
            {
                _bugRenderer.Render(dc, _world.Bugs[i]);
            }

            _spiderRenderer.Render(dc, _spider);
        }
    }
}
