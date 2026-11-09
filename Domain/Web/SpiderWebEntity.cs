using System;
using System.Collections.Generic;
using System.Windows;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Domain.Web
{
    /// <summary>
    /// Örümcek ağının durumunu, titreşim/rüzgar fiziğini, hasar/onarım kuyruğunu ve etkileşim geometrisini yönetir.
    /// </summary>
    public class SpiderWebEntity
    {
        public Point Center { get; }
        public Point HubPosition { get; }
        public double Radius { get; }
        public double StartAngle { get; }
        public double EndAngle { get; }
        public bool IsCornerFan => (EndAngle - StartAngle) < Math.PI;

        public List<WebSegment> CompletedSegments { get; } = new();
        public Queue<WebBuildStep> BuildQueue { get; }
        public Queue<WebSegment> DamagedSegments { get; } = new();
        public WebDropletController Droplets { get; } = new();
        public WebPantryController Pantry { get; } = new();

        public bool IsStepInProgress { get; set; }
        public bool IsCompleted => BuildQueue.Count == 0 && !IsStepInProgress;
        public bool HasDamage => DamagedSegments.Count > 0;

        public double VibrationAmplitude { get; private set; }
        public double VibrationPhase { get; private set; }
        public Point VibrationOrigin { get; private set; }

        public double BreezeOffset { get; private set; }
        public Vector BreezeDirection { get; private set; }
        private double _breezeCooldown = SpiderTuning.BreezeCooldownBase;
        private double _breezeDuration = SpiderTuning.BreezeDurationBase;
        private double _breezeElapsedTime = 999.0;
        private double _breezeMaxStrength = SpiderTuning.BreezeStrengthBase;

        public bool IsVibrating => VibrationAmplitude > SpiderTuning.WebMotionThreshold;
        public bool IsBreezing => Math.Abs(BreezeOffset) > SpiderTuning.WebMotionThreshold;

        /// <summary>Ağın telleri (ve üzerindeki varlıklar) şu an rüzgar veya titreşimle kayıyorsa true döner.</summary>
        public bool IsDeforming => IsVibrating || IsBreezing;

        /// <summary>
        /// Ağ her karede yeniden çizilmeyi gerektiren bir hareket içindeyse (titreşim, rüzgar veya düşen damla) true döner.
        /// </summary>
        public bool IsAnimating => IsDeforming || Droplets.FallingDroplets.Count > 0;

        public Point? RecentCursorPluckPoint { get; set; }

        private double _pluckCooldown;

        public SpiderWebEntity(
            Point center,
            Point hubPosition,
            double radius,
            Queue<WebBuildStep> buildPlan,
            double startAngle = 0.0,
            double endAngle = Math.PI * 2.0)
        {
            Center = center;
            HubPosition = hubPosition;
            Radius = radius;
            BuildQueue = buildPlan;
            StartAngle = startAngle;
            EndAngle = endAngle;
        }

        public void TriggerVibration(Point origin, double strength = 6.0)
        {
            VibrationOrigin = origin;
            VibrationAmplitude = Math.Min(10.0, Math.Max(VibrationAmplitude, strength));
        }

        public void Update(double dt, Random rng)
        {
            if (_pluckCooldown > 0)
            {
                _pluckCooldown -= dt;
            }

            // Periyodik doğal rüzgar esintisi (Breeze):
            if (!IsCompleted)
            {
                BreezeOffset = 0;
            }
            else if (_breezeElapsedTime < _breezeDuration)
            {
                _breezeElapsedTime += dt;
                double progress = _breezeElapsedTime / _breezeDuration;
                if (progress >= 1.0)
                {
                    BreezeOffset = 0;
                    _breezeCooldown = SpiderTuning.BreezeCooldownBase + rng.NextDouble() * SpiderTuning.BreezeCooldownVariance;
                }
                else
                {
                    // Yumuşak zarf eğrisi (Envelope): 0 -> 1 -> 0
                    double envelope = Math.Sin(progress * Math.PI);
                    // 1.5 salınım döngüsü (sağa tatlıca esner, yay gibi hafifçe geri gelir ve durulur)
                    double wave = Math.Sin(progress * Math.PI * 3.0);
                    BreezeOffset = envelope * wave * _breezeMaxStrength;
                }
            }
            else
            {
                _breezeCooldown -= dt;
                if (_breezeCooldown <= 0)
                {
                    _breezeElapsedTime = 0;
                    _breezeDuration = SpiderTuning.BreezeDurationBase + rng.NextDouble() * SpiderTuning.BreezeDurationVariance;
                    _breezeMaxStrength = SpiderTuning.BreezeStrengthBase + rng.NextDouble() * SpiderTuning.BreezeStrengthVariance;
                    double dirX = rng.NextDouble() > 0.5 ? 1.0 : -1.0;
                    double dirY = (rng.NextDouble() - 0.4) * 0.5;
                    Vector dir = new Vector(dirX, dirY);
                    if (dir.Length > 0.001) dir.Normalize();
                    BreezeDirection = dir;
                }
            }

            Droplets.Update(dt, IsCompleted, CompletedSegments, rng);

            if (IsVibrating)
            {
                VibrationPhase += dt * 38.0;
                VibrationAmplitude *= Math.Pow(0.18, dt);
            }
            else
            {
                VibrationAmplitude = 0;
            }
        }

        public void DamageNear(Point impactPoint, double damageRadius, int maxBrokenSegments)
        {
            if (!IsCompleted || maxBrokenSegments <= 0) return;

            double radiusSq = damageRadius * damageRadius;
            int brokenCount = 0;

            for (int i = CompletedSegments.Count - 1; i >= 0 && brokenCount < maxBrokenSegments; i--)
            {
                var seg = CompletedSegments[i];
                if (seg.IsAnchor) continue;

                if (DistanceToSegmentSquared(impactPoint, seg.Start, seg.End) <= radiusSq)
                {
                    // Tel kopmadan önce üzerindeki çiy damlası varsa onu serbest bırakıp düşür!
                    Droplets.OnSegmentBroken(i, CompletedSegments, Center, new Random());

                    CompletedSegments.RemoveAt(i);
                    DamagedSegments.Enqueue(seg);
                    brokenCount++;
                }
            }

            if (brokenCount > 0)
            {
                TriggerVibration(impactPoint, 9.0);
            }
        }

        public void CheckCursorPluck(Point cursorPos, double cursorSpeed)
        {
            if (!IsCompleted || cursorSpeed < 25.0 || _pluckCooldown > 0) return;

            Vector toCenter = cursorPos - Center;
            if (toCenter.LengthSquared > Radius * Radius * 1.35) return;

            foreach (var segment in CompletedSegments)
            {
                if (DistanceToSegmentSquared(cursorPos, segment.Start, segment.End) < SpiderTuning.WebCursorHitDistanceSquared)
                {
                    if (cursorSpeed > SpiderTuning.WebCursorTearSpeed)
                    {
                        DamageNear(cursorPos, SpiderTuning.WebCursorDamageRadius, maxBrokenSegments: 2);
                    }
                    else
                    {
                        double strength = Math.Clamp(cursorSpeed * 0.012, 2.5, 7.5);
                        TriggerVibration(cursorPos, strength);
                        RecentCursorPluckPoint = cursorPos;
                    }

                    _pluckCooldown = SpiderTuning.WebPluckCooldownSeconds;
                    break;
                }
            }
        }

        /// <summary>
        /// Ağın durgun görünümünü etkileyen durumun (teller, kopuk teller, damlalar, kozalar) özet değeri.
        /// Değer değişmediyse ve ağ hareket etmiyorsa, önceki çizim aynen geçerlidir.
        /// </summary>
        public int ComputeVisualStateHash()
        {
            var hash = new HashCode();
            hash.Add(CompletedSegments.Count);
            hash.Add(DamagedSegments.Count);
            hash.Add(Droplets.Revision);

            var cocoons = Pantry.StoredCocoons;
            hash.Add(cocoons.Count);
            for (int i = 0; i < cocoons.Count; i++)
            {
                hash.Add((int)(cocoons[i].ConsumeProgress * 100));
            }

            return hash.ToHashCode();
        }

        public bool CanCatchPoint(Point point)
        {
            if (!IsCompleted) return false;

            Vector toCenter = point - Center;
            if (toCenter.LengthSquared > Radius * Radius * 1.35) return false;

            foreach (var segment in CompletedSegments)
            {
                if (DistanceToSegmentSquared(point, segment.Start, segment.End) < SpiderTuning.WebCatchDistanceSquared)
                {
                    return true;
                }
            }
            return false;
        }

        private static double DistanceToSegmentSquared(Point p, Point a, Point b)
        {
            Vector ab = b - a;
            double lenSq = ab.LengthSquared;
            if (lenSq < 0.0001) return (p - a).LengthSquared;

            double t = ((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / lenSq;
            t = Math.Clamp(t, 0.0, 1.0);

            Point projection = a + ab * t;
            return (p - projection).LengthSquared;
        }
    }
}
