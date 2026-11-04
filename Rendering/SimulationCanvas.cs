using System.Windows.Controls;
using SpiderDesk.Core;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Spider;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Durağan ağ katmanı (WebLayerCanvas) ile hareketli karakter katmanını (ActorLayerCanvas)
    /// bir araya getiren ana görsel bileşen. Her katman yalnızca kendi verisi değiştiğinde güncellenir.
    /// </summary>
    public class SimulationCanvas : Grid
    {
        private readonly WebLayerCanvas _webLayer;
        private readonly ActorLayerCanvas _actorLayer;

        public SimulationCanvas(
            WorldContext world,
            SpiderEntity spider,
            IRenderer<SpiderWebEntity> webRenderer,
            IRenderer<BugEntity> bugRenderer,
            IRenderer<SpiderEntity> spiderRenderer)
        {
            IsHitTestVisible = false;

            _webLayer = new WebLayerCanvas(world, webRenderer);
            _actorLayer = new ActorLayerCanvas(world, spider, bugRenderer, spiderRenderer);

            Children.Add(_webLayer);
            Children.Add(_actorLayer);
        }

        public void RefreshLayers(bool forceAll = false)
        {
            _webLayer.RefreshIfNeeded(forceAll);
            _actorLayer.RefreshIfNeeded(forceAll);
        }
    }
}
