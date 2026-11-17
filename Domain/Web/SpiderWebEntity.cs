using System;
using System.Collections.Generic;
using System.Windows;

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

        private readonly List<WebSegment> _completedSegments = new();
        private readonly Queue<WebBuildStep> _buildQueue;
        private readonly Queue<WebSegment> _damagedSegments = new();

        /// <summary>Gerilmiş teller. Dışarıdan yalnızca okunur; değiştirmek için ağın metotları kullanılır.</summary>
        public IReadOnlyList<WebSegment> CompletedSegments => _completedSegments;

        /// <summary>Kopmuş ve tamir edilmeyi bekleyen teller (sırayla).</summary>
        public IReadOnlyCollection<WebSegment> DamagedSegments => _damagedSegments;

        public WebDropletController Droplets { get; } = new();
        public WebPantryController Pantry { get; } = new();

        public bool IsStepInProgress { get; private set; }
        public bool IsCompleted => _buildQueue.Count == 0 && !IsStepInProgress;
        public bool HasDamage => _damagedSegments.Count > 0;

        public double VibrationAmplitude { get; private set; }
        public double VibrationPhase { get; private set; }
        public Point VibrationOrigin { get; private set; }

        public double BreezeOffset { get; private set; }
        public Vector BreezeDirection { get; private set; }
        private double _breezeCooldown = WebTuning.BreezeCooldownBase;
        private double _breezeDuration = WebTuning.BreezeDurationBase;
        private double _breezeElapsedTime = 999.0;
        private double _breezeMaxStrength = WebTuning.BreezeStrengthBase;

        public bool IsVibrating => VibrationAmplitude > WebTuning.MotionThreshold;
        public bool IsBreezing => Math.Abs(BreezeOffset) > WebTuning.MotionThreshold;

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
            _buildQueue = buildPlan;
            StartAngle = startAngle;
            EndAngle = endAngle;
        }

        // ---------- Örülme ----------

        /// <summary>
        /// Sıradaki örme adımını başlatır ve onu verir. Örülecek adım kalmadıysa false döner.
        /// Adım bitene kadar ağ tamamlanmış sayılmaz.
        /// </summary>
        public bool TryBeginNextStep(out WebBuildStep step)
        {
            IsStepInProgress = _buildQueue.TryPeek(out step);
            return IsStepInProgress;
        }

        /// <summary>Başlatılan adımı bitirir; adım ipek bırakıyorsa yeni tel ağa eklenir.</summary>
        public void CompleteCurrentStep()
        {
            if (_buildQueue.TryDequeue(out WebBuildStep step) && step.DrawSilk)
            {
                _completedSegments.Add(new WebSegment(step.From, step.To, step.IsAnchor));
            }
            IsStepInProgress = false;
        }

        /// <summary>Örme yarıda kesildiğinde çağrılır; adım sırada kalır ve sonra kaldığı yerden devam edilir.</summary>
        public void InterruptCurrentStep() => IsStepInProgress = false;

        // ---------- Tamir ----------

        /// <summary>Tamir edilmeyi bekleyen sıradaki kopuk teli alır. Kopuk tel yoksa false döner.</summary>
        public bool TryTakeDamagedSegment(out WebSegment segment) => _damagedSegments.TryDequeue(out segment);

        /// <summary>Tamir edilen teli ağa geri takar.</summary>
        public void RestoreSegment(WebSegment segment) => _completedSegments.Add(segment);

        /// <summary>Tamir yarıda kaldığında alınan teli, sonra tamir edilmek üzere kuyruğa geri koyar.</summary>
        public void ReturnDamagedSegment(WebSegment segment) => _damagedSegments.Enqueue(segment);

        public void TriggerVibration(Point origin, double strength = WebTuning.DefaultVibrationStrength)
        {
            VibrationOrigin = origin;
            VibrationAmplitude = Math.Min(WebTuning.MaxVibrationAmplitude, Math.Max(VibrationAmplitude, strength));
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
                    _breezeCooldown = WebTuning.BreezeCooldownBase + rng.NextDouble() * WebTuning.BreezeCooldownVariance;
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
                    _breezeDuration = WebTuning.BreezeDurationBase + rng.NextDouble() * WebTuning.BreezeDurationVariance;
                    _breezeMaxStrength = WebTuning.BreezeStrengthBase + rng.NextDouble() * WebTuning.BreezeStrengthVariance;
                    double dirX = rng.NextDouble() > 0.5 ? 1.0 : -1.0;
                    double dirY = (rng.NextDouble() - 0.4) * 0.5;
                    Vector dir = new Vector(dirX, dirY);
                    if (dir.Length > 0.001) dir.Normalize();
                    BreezeDirection = dir;
                }
            }

            Droplets.Update(dt, IsCompleted, _completedSegments, rng);

            if (IsVibrating)
            {
                VibrationPhase += dt * WebTuning.VibrationFrequency;
                VibrationAmplitude *= Math.Pow(WebTuning.VibrationDecayPerSecond, dt);
            }
            else
            {
                VibrationAmplitude = 0;
            }
        }

        public void DamageNear(Point impactPoint, double damageRadius, int maxBrokenSegments, Random rng)
        {
            if (!IsCompleted || maxBrokenSegments <= 0) return;

            double radiusSq = damageRadius * damageRadius;
            int brokenCount = 0;

            for (int i = _completedSegments.Count - 1; i >= 0 && brokenCount < maxBrokenSegments; i--)
            {
                var seg = _completedSegments[i];
                if (seg.IsAnchor) continue;

                if (DistanceToSegmentSquared(impactPoint, seg.Start, seg.End) <= radiusSq)
                {
                    // Tel kopmadan önce üzerindeki çiy damlası varsa onu serbest bırakıp düşür!
                    Droplets.OnSegmentBroken(i, _completedSegments, Center, rng);

                    _completedSegments.RemoveAt(i);
                    _damagedSegments.Enqueue(seg);
                    brokenCount++;
                }
            }

            if (brokenCount > 0)
            {
                TriggerVibration(impactPoint, WebTuning.TearVibrationStrength);
            }
        }

        public void CheckCursorPluck(Point cursorPos, double cursorSpeed, Random rng)
        {
            if (!IsCompleted || cursorSpeed < WebTuning.PluckMinCursorSpeed || _pluckCooldown > 0) return;

            Vector toCenter = cursorPos - Center;
            if (toCenter.LengthSquared > Radius * Radius * WebTuning.InteractionRadiusSquaredFactor) return;

            foreach (var segment in _completedSegments)
            {
                if (DistanceToSegmentSquared(cursorPos, segment.Start, segment.End) < WebTuning.CursorHitDistanceSquared)
                {
                    if (cursorSpeed > WebTuning.CursorTearSpeed)
                    {
                        DamageNear(cursorPos, WebTuning.CursorDamageRadius, maxBrokenSegments: 2, rng);
                    }
                    else
                    {
                        double strength = Math.Clamp(cursorSpeed * WebTuning.PluckStrengthPerCursorSpeed, WebTuning.MinPluckStrength, WebTuning.MaxPluckStrength);
                        TriggerVibration(cursorPos, strength);
                        RecentCursorPluckPoint = cursorPos;
                    }

                    _pluckCooldown = WebTuning.PluckCooldownSeconds;
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
            hash.Add(_completedSegments.Count);
            hash.Add(_damagedSegments.Count);
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
            if (toCenter.LengthSquared > Radius * Radius * WebTuning.InteractionRadiusSquaredFactor) return false;

            foreach (var segment in _completedSegments)
            {
                if (DistanceToSegmentSquared(point, segment.Start, segment.End) < WebTuning.CatchDistanceSquared)
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
