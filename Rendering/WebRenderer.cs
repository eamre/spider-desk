using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using SpiderDesk.Core;
using SpiderDesk.Domain.Spider.Themes;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Örümcek ağlarını (teller, çiy, su damlaları, kopuk teller ve kilerdeki kozalar) aktif temaya göre çizer.
    /// Tel geometrisini <see cref="WebGeometryBuilder"/> üretir: durgun ağ önbellekten, sallanan ağ o karenin
    /// <see cref="WebDeformation"/>'ı ile yeniden üretilerek kalem başına tek çizim çağrısıyla çizilir.
    /// </summary>
    public class WebRenderer : IRenderer<SpiderWebEntity>
    {
        private sealed class StillSilkCache
        {
            public int SegmentCount = -1;
            public int DamagedCount;
            public int DropletRevision;
            public WebSilkGeometry Silk = null!;
        }

        private readonly Pen _brokenStubPen = DrawingResources.CreateFrozenPen(Color.FromArgb(150, 255, 210, 210), 1.0);
        private readonly Pen _cocoonSilkPen = DrawingResources.CreateFrozenPen(Color.FromArgb(225, 255, 255, 255), 1.2);
        private readonly SolidColorBrush _cocoonFillBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(230, 242, 248, 255));
        private readonly SolidColorBrush _dewBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(180, 255, 255, 255));
        private readonly SolidColorBrush _waterDropBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(205, 195, 238, 255));
        private readonly SolidColorBrush _waterHighlightBrush = DrawingResources.CreateFrozenBrush(Color.FromArgb(245, 255, 255, 255));
        private readonly Dictionary<byte, SolidColorBrush> _fallingDropBrushes = new();
        private readonly ConditionalWeakTable<SpiderWebEntity, StillSilkCache> _stillSilkCache = new();

        private Pen _anchorPen = null!;
        private Pen _spiralPen = null!;

        public WebRenderer()
        {
            ApplyTheme(SpiderThemeCatalog.All[0]);
        }

        public void ApplyTheme(ISpiderTheme theme)
        {
            _anchorPen = DrawingResources.CreateFrozenPen(theme.WebAnchorColor, 1.35);
            _spiralPen = DrawingResources.CreateFrozenPen(theme.WebSpiralColor, 0.95);
        }

        public void Render(DrawingContext dc, SpiderWebEntity web)
        {
            var deformation = WebDeformation.Current(web);

            WebSilkGeometry silk = deformation.IsStill
                ? GetStillSilk(web)
                : WebGeometryBuilder.BuildSilk(web, deformation);

            dc.DrawGeometry(null, _anchorPen, silk.Anchors);
            dc.DrawGeometry(null, _spiralPen, silk.Spirals);
            dc.DrawGeometry(_dewBrush, null, silk.Dew);

            DrawAttachedDroplets(dc, web, deformation);
            DrawFallingDroplets(dc, web);
            DrawBrokenStubs(dc, web, deformation);
            DrawCocoons(dc, web, deformation);
        }

        private WebSilkGeometry GetStillSilk(SpiderWebEntity web)
        {
            StillSilkCache cache = _stillSilkCache.GetValue(web, _ => new StillSilkCache());

            int segmentCount = web.CompletedSegments.Count;
            int damagedCount = web.DamagedSegments.Count;
            int dropletRevision = web.Droplets.Revision;

            if (cache.SegmentCount != segmentCount ||
                cache.DamagedCount != damagedCount ||
                cache.DropletRevision != dropletRevision)
            {
                cache.SegmentCount = segmentCount;
                cache.DamagedCount = damagedCount;
                cache.DropletRevision = dropletRevision;
                cache.Silk = WebGeometryBuilder.BuildSilk(web, WebDeformation.Still(web));
            }

            return cache.Silk;
        }

        /// <summary>Tellerin üzerindeki ağır su/yağmur damlaları.</summary>
        private void DrawAttachedDroplets(DrawingContext dc, SpiderWebEntity web, in WebDeformation deformation)
        {
            var droplets = web.Droplets.AttachedDroplets;
            for (int i = 0; i < droplets.Count; i++)
            {
                var drop = droplets[i];
                Point pos = deformation.Displace(
                    web.Droplets.CalculateDropletWorldPosition(drop, web.CompletedSegments, web.Center));

                double r = drop.Radius;
                dc.DrawEllipse(_waterDropBrush, null, pos, r * 0.92, r * 1.12);
                dc.DrawEllipse(_waterHighlightBrush, null, new Point(pos.X - r * 0.28, pos.Y - r * 0.3), r * 0.32, r * 0.32);
            }
        }

        /// <summary>Örümcek ağı silkelediğinde aşağı düşen su damlaları.</summary>
        private void DrawFallingDroplets(DrawingContext dc, SpiderWebEntity web)
        {
            var droplets = web.Droplets.FallingDroplets;
            for (int i = 0; i < droplets.Count; i++)
            {
                var falling = droplets[i];
                byte alpha = (byte)((int)(falling.Opacity * 200) / 10 * 10);
                if (alpha < 10) continue;

                double r = falling.Radius;
                dc.DrawEllipse(GetFallingDropBrush(alpha), null, falling.FallingPosition, r * 0.78, r * 1.35);
            }
        }

        /// <summary>Kopmuş tellerin sarkan uçları.</summary>
        private void DrawBrokenStubs(DrawingContext dc, SpiderWebEntity web, in WebDeformation deformation)
        {
            foreach (var broken in web.DamagedSegments)
            {
                Vector direction = broken.End - broken.Start;
                Point start = broken.Start;
                Point danglingTip = new(start.X + direction.X * 0.25, start.Y + direction.Y * 0.25 + 6.0);

                // Uç, tutunduğu noktayla birlikte aynı miktarda kayar
                Vector shift = deformation.Displace(start) - start;
                dc.DrawLine(_brokenStubPen, start + shift, danglingTip + shift);
            }
        }

        /// <summary>Kilerde (erzak deposunda) asılı duran ipek kozalar; tek geometri, tek çizim çağrısı.</summary>
        private void DrawCocoons(DrawingContext dc, SpiderWebEntity web, in WebDeformation deformation)
        {
            if (web.Pantry.StoredCocoons.Count == 0) return;

            dc.DrawGeometry(_cocoonFillBrush, _cocoonSilkPen, WebGeometryBuilder.BuildCocoons(web, deformation));
        }

        private SolidColorBrush GetFallingDropBrush(byte alpha)
        {
            if (!_fallingDropBrushes.TryGetValue(alpha, out var brush))
            {
                brush = DrawingResources.CreateFrozenBrush(Color.FromArgb(alpha, 195, 238, 255));
                _fallingDropBrushes[alpha] = brush;
            }
            return brush;
        }
    }
}
