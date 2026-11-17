using System.Windows;
using System.Windows.Media;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.SilkText;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Yalnızca hareketli varlıkları (örümcek, uçan/yakalanan böcekler ve ipekle yazılan mesaj) çizen katman.
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
        private bool _showBugs = true;

        /// <summary>False iken böcekler çizilmez; örümcek ve ipek yazısı çizilmeye devam eder.</summary>
        public bool ShowBugs
        {
            get => _showBugs;
            set
            {
                if (_showBugs == value) return;
                _showBugs = value;
                InvalidateVisual();
            }
        }

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
                || _world.SilkMessage != null
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

            if (_world.SilkMessage is { } message)
            {
                _silkWritingRenderer.Render(dc, message);
            }

            if (_showBugs)
            {
                for (int i = 0; i < _world.Bugs.Count; i++)
                {
                    _bugRenderer.Render(dc, _world.Bugs[i]);
                }
            }

            _spiderRenderer.Render(dc, _spider);
        }
    }
}
