using System;
using System.Collections.Generic;
using System.Windows;
using SpiderDesk.Core;

namespace SpiderDesk.Domain.Web
{
    /// <summary>
    /// Bir ağ üzerindeki su damlalarının oluşumunu, tel esnemesini (yerçekimi sarkması)
    /// ve silkelenme fiziğini yönetir.
    /// </summary>
    public class WebDropletController
    {
        private readonly List<WaterDroplet> _attachedDroplets = new();
        private readonly List<WaterDroplet> _fallingDroplets = new();
        private double _condensationTimer = 14.0;
        private DewSpawnRatePreset _spawnRate = DewSpawnRatePreset.Normal;

        public IReadOnlyList<WaterDroplet> AttachedDroplets => _attachedDroplets;
        public IReadOnlyList<WaterDroplet> FallingDroplets => _fallingDroplets;

        public DewSpawnRatePreset SpawnRate
        {
            get => _spawnRate;
            set
            {
                _spawnRate = value;
                if (_spawnRate != DewSpawnRatePreset.Off)
                {
                    _condensationTimer = Math.Min(_condensationTimer, GetNextInterval(new Random()));
                }
            }
        }

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

            if (!isWebCompleted || segments.Count == 0 || _spawnRate == DewSpawnRatePreset.Off)
            {
                return;
            }

            // 2. Mevcut damlalar zamanla çiy biriktirerek biraz büyür
            foreach (var drop in _attachedDroplets)
            {
                drop.Grow(dt * 0.08);
            }

            // 3. Seçili çiy sıklığına göre yeni çiy damlaları oluşur
            _condensationTimer -= dt;
            if (_condensationTimer <= 0)
            {
                _condensationTimer = GetNextInterval(rng);
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
                    totalSag += _attachedDroplets[i].Radius * 2.4;
                }
            }
            return Math.Min(14.0, totalSag);
        }

        public Point CalculateDropletWorldPosition(WaterDroplet droplet, IReadOnlyList<WebSegment> segments, Point webCenter)
        {
            if (droplet.SegmentIndex < 0 || droplet.SegmentIndex >= segments.Count)
            {
                return new Point(0, 0);
            }

            var seg = segments[droplet.SegmentIndex];
            double t = droplet.NormalizedT;
            double sag = GetSegmentSag(droplet.SegmentIndex);

            Point mid = new Point((seg.Start.X + seg.End.X) * 0.5, (seg.Start.Y + seg.End.Y) * 0.5);
            double segLen = (seg.End - seg.Start).Length;

            Vector toCenter = webCenter - mid;
            double distToCenter = toCenter.Length;
            if (distToCenter > 0.001)
            {
                toCenter /= distToCenter;
            }

            double inwardPull = seg.IsAnchor ? 0.0 : Math.Min(11.0, segLen * 0.13);
            Point control = new Point(
                mid.X + toCenter.X * inwardPull * 1.8,
                mid.Y + toCenter.Y * inwardPull * 1.8 + sag * 1.6 + 0.6
            );

            double inv = 1.0 - t;
            return new Point(
                inv * inv * seg.Start.X + 2.0 * inv * t * control.X + t * t * seg.End.X,
                inv * inv * seg.Start.Y + 2.0 * inv * t * control.Y + t * t * seg.End.Y
            );
        }

        public void ShakeOffAllDroplets(IReadOnlyList<WebSegment> segments, Point webCenter, Random rng)
        {
            foreach (var drop in _attachedDroplets)
            {
                if (drop.SegmentIndex >= 0 && drop.SegmentIndex < segments.Count)
                {
                    Point worldPos = CalculateDropletWorldPosition(drop, segments, webCenter);
                    double initialVy = 45.0 + rng.NextDouble() * 65.0;
                    drop.Detach(worldPos, initialVy);
                    _fallingDroplets.Add(drop);
                }
            }
            _attachedDroplets.Clear();
        }

        private void SpawnDropletOnRandomSegment(IReadOnlyList<WebSegment> segments, Random rng, double initialRadius = 2.0)
        {
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

        private double GetNextInterval(Random rng)
        {
            return _spawnRate switch
            {
                DewSpawnRatePreset.Low => 30.0 + rng.NextDouble() * 20.0,    // 30 - 50 sn
                DewSpawnRatePreset.Normal => 14.0 + rng.NextDouble() * 12.0, // 14 - 26 sn
                DewSpawnRatePreset.High => 5.0 + rng.NextDouble() * 5.0,     // 5 - 10 sn
                _ => 999.0
            };
        }
    }
}
