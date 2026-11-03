using System;
using System.Collections.Generic;
using System.Windows;

namespace SpiderDesk.Domain.Web
{
    /// <summary>
    /// Bir ağ üzerindeki su damlalarının oluşumunu, tel esnemesini (yerçekimi sarkması)
    /// ve silkelenme fiziğini yönetir (SRP).
    /// </summary>
    public class WebDropletController
    {
        private readonly List<WaterDroplet> _attachedDroplets = new();
        private readonly List<WaterDroplet> _fallingDroplets = new();
        private double _condensationTimer = 14.0;

        public IReadOnlyList<WaterDroplet> AttachedDroplets => _attachedDroplets;
        public IReadOnlyList<WaterDroplet> FallingDroplets => _fallingDroplets;

        /// <summary>
        /// Ağda yeterince ağır su damlası biriktiğinde örümceğin ağı silkelemesi gerektiğini bildirir.
        /// </summary>
        public bool IsHeavyWithWater => _attachedDroplets.Count >= 6;

        public void Update(double dt, bool isWebCompleted, IReadOnlyList<WebSegment> segments, Random rng)
        {
            // 1. Düşmekte olan damlaları güncelle
            for (int i = _fallingDroplets.Count - 1; i >= 0; i--)
            {
                _fallingDroplets[i].UpdateFalling(dt);
                if (_fallingDroplets[i].Opacity <= 0.02)
                {
                    _fallingDroplets.RemoveAt(i);
                }
            }

            if (!isWebCompleted || segments.Count == 0) return;

            // 2. Mevcut damlalar zamanla çiy biriktirerek biraz büyür
            foreach (var drop in _attachedDroplets)
            {
                drop.Grow(dt * 0.08);
            }

            // 3. Zamanla yeni çiy damlaları oluşur
            _condensationTimer -= dt;
            if (_condensationTimer <= 0)
            {
                _condensationTimer = 12.0 + rng.NextDouble() * 14.0;
                if (_attachedDroplets.Count < 9)
                {
                    SpawnDropletOnRandomSegment(segments, rng);
                }
            }
        }

        public void AddRainBurst(IReadOnlyList<WebSegment> segments, Random rng, int count = 6)
        {
            if (segments.Count == 0) return;

            for (int i = 0; i < count && _attachedDroplets.Count < 12; i++)
            {
                SpawnDropletOnRandomSegment(segments, rng, initialRadius: 2.8 + rng.NextDouble() * 0.9);
            }
        }

        public double GetSegmentSag(int segmentIndex)
        {
            double totalSag = 0;
            for (int i = 0; i < _attachedDroplets.Count; i++)
            {
                if (_attachedDroplets[i].SegmentIndex == segmentIndex)
                {
                    // Damla büyüklüğüne göre tel aşağı doğru esner
                    totalSag += _attachedDroplets[i].Radius * 2.4;
                }
            }
            return Math.Min(14.0, totalSag);
        }

        public Point CalculateDropletWorldPosition(WaterDroplet droplet, IReadOnlyList<WebSegment> segments)
        {
            if (droplet.SegmentIndex < 0 || droplet.SegmentIndex >= segments.Count)
            {
                return new Point(0, 0);
            }

            var seg = segments[droplet.SegmentIndex];
            double t = droplet.NormalizedT;
            double sag = GetSegmentSag(droplet.SegmentIndex);

            // Parabolik esneme eğrisi: 4 * t * (1 - t) orta noktada maksimum (1.0) değer alır
            double sagOffset = 4.0 * t * (1.0 - t) * sag;

            return new Point(
                seg.Start.X + (seg.End.X - seg.Start.X) * t,
                seg.Start.Y + (seg.End.Y - seg.Start.Y) * t + sagOffset
            );
        }

        public void ShakeOffAllDroplets(IReadOnlyList<WebSegment> segments, Random rng)
        {
            foreach (var drop in _attachedDroplets)
            {
                if (drop.SegmentIndex >= 0 && drop.SegmentIndex < segments.Count)
                {
                    Point worldPos = CalculateDropletWorldPosition(drop, segments);
                    double initialVy = 45.0 + rng.NextDouble() * 65.0;
                    drop.Detach(worldPos, initialVy);
                    _fallingDroplets.Add(drop);
                }
            }
            _attachedDroplets.Clear();
        }

        private void SpawnDropletOnRandomSegment(IReadOnlyList<WebSegment> segments, Random rng, double initialRadius = 2.0)
        {
            // Sarmal telleri tercih et
            for (int attempt = 0; attempt < 10; attempt++)
            {
                int idx = rng.Next(segments.Count);
                if (!segments[idx].IsAnchor)
                {
                    double t = 0.25 + rng.NextDouble() * 0.5;
                    _attachedDroplets.Add(new WaterDroplet(idx, t, initialRadius));
                    return;
                }
            }
        }
    }
}
