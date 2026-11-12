using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Greeting;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Yalnızca hareketli varlıkları (örümcek, uçan/yakalanan böcekler ve açılışta ipekle yazılan selam) çizen katman.
    /// Ağ katmanından bağımsız güncellenerek her karede tüm ağların yeniden taranmasını önler.
    /// </summary>
    public class ActorLayerCanvas : FrameworkElement
    {
        private readonly WorldContext _world;
        private readonly SpiderEntity _spider;
        private readonly IRenderer<BugEntity> _bugRenderer;
        private readonly IRenderer<SpiderEntity> _spiderRenderer;
        private readonly IRenderer<SilkWriting> _silkWritingRenderer;
        private bool _wasIdle;

        public ActorLayerCanvas(
            WorldContext world,
            SpiderEntity spider,
            IRenderer<BugEntity> bugRenderer,
            IRenderer<SpiderEntity> spiderRenderer,
            IRenderer<SilkWriting> silkWritingRenderer)
        {
            IsHitTestVisible = false;
            _world = world;
            _spider = spider;
            _bugRenderer = bugRenderer;
            _spiderRenderer = spiderRenderer;
            _silkWritingRenderer = silkWritingRenderer;
        }

        public void RefreshIfNeeded(bool force = false)
        {
            bool hasActorMotion = _spider.HasVisualMotion
                || _world.Bugs.Count > 0
                || _world.Greeting != null
                || _spider.ActiveWeb?.IsDeforming == true;
            if (force || hasActorMotion || !_wasIdle)
            {
                _wasIdle = !hasActorMotion;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (_world.Greeting is { } greeting)
            {
                _silkWritingRenderer.Render(dc, greeting);
            }

            for (int i = 0; i < _world.Bugs.Count; i++)
            {
                _bugRenderer.Render(dc, _world.Bugs[i]);
            }

            _spiderRenderer.Render(dc, _spider);
        }
    }
}
