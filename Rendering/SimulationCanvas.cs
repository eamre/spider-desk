using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Simülasyon dünyasını kendisine enjekte edilen IRenderer uygulamalarıyla çizer (DIP & SRP).
    /// </summary>
    public class SimulationCanvas : FrameworkElement
    {
        private readonly WorldContext _world;
        private readonly SpiderEntity _spider;
        private readonly IRenderer<SpiderWebEntity> _webRenderer;
        private readonly IRenderer<BugEntity> _bugRenderer;
        private readonly IRenderer<SpiderEntity> _spiderRenderer;

        public SimulationCanvas(
            WorldContext world,
            SpiderEntity spider,
            IRenderer<SpiderWebEntity> webRenderer,
            IRenderer<BugEntity> bugRenderer,
            IRenderer<SpiderEntity> spiderRenderer)
        {
            IsHitTestVisible = false;
            _world = world;
            _spider = spider;
            _webRenderer = webRenderer;
            _bugRenderer = bugRenderer;
            _spiderRenderer = spiderRenderer;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            foreach (var web in _world.Webs)
            {
                _webRenderer.Render(dc, web);
            }

            foreach (var bug in _world.Bugs)
            {
                _bugRenderer.Render(dc, bug);
            }

            _spiderRenderer.Render(dc, _spider);
        }
    }
}
