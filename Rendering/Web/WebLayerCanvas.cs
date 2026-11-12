using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering.Web
{
    /// <summary>
    /// Örümcek ağlarını, çiy damlalarını ve kilerdeki kozaları barındıran katman.
    /// Kendisi çizim yapmaz; her ağ için ayrı bir <see cref="WebVisual"/> tutar ve görsel listesini dünyadaki
    /// ağlarla eşitler. Böylece yalnızca değişen veya hareket eden ağ yeniden çizilir.
    /// </summary>
    public class WebLayerCanvas : FrameworkElement
    {
        private readonly WorldContext _world;
        private readonly IRenderer<SpiderWebEntity> _webRenderer;
        private readonly VisualCollection _webVisuals;

        public WebLayerCanvas(WorldContext world, IRenderer<SpiderWebEntity> webRenderer)
        {
            // Koleksiyon ilk sırada oluşturulmalı: IsHitTestVisible gibi özellik atamaları WPF'in
            // VisualChildrenCount'u hemen sorgulamasına yol açar.
            _webVisuals = new VisualCollection(this);
            _world = world;
            _webRenderer = webRenderer;
            IsHitTestVisible = false;
        }

        public void RefreshIfNeeded(bool force = false)
        {
            if (!IsInSyncWithWorld())
            {
                RebuildVisualList();
            }

            for (int i = 0; i < _webVisuals.Count; i++)
            {
                ((WebVisual)_webVisuals[i]).RefreshIfNeeded(force);
            }
        }

        protected override int VisualChildrenCount => _webVisuals.Count;

        protected override Visual GetVisualChild(int index) => _webVisuals[index];

        private bool IsInSyncWithWorld()
        {
            if (_webVisuals.Count != _world.Webs.Count) return false;

            for (int i = 0; i < _webVisuals.Count; i++)
            {
                if (((WebVisual)_webVisuals[i]).Web != _world.Webs[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Ağ eklendiğinde, silindiğinde veya sırası değiştiğinde çalışır (nadir).
        /// Var olan ağların görselleri korunur; yalnızca yeni ağlar için görsel oluşturulur.
        /// </summary>
        private void RebuildVisualList()
        {
            var existing = new Dictionary<SpiderWebEntity, WebVisual>(_webVisuals.Count);
            for (int i = 0; i < _webVisuals.Count; i++)
            {
                var visual = (WebVisual)_webVisuals[i];
                existing[visual.Web] = visual;
            }

            _webVisuals.Clear();
            foreach (var web in _world.Webs)
            {
                _webVisuals.Add(existing.TryGetValue(web, out var visual)
                    ? visual
                    : new WebVisual(web, _webRenderer));
            }
        }
    }
}
