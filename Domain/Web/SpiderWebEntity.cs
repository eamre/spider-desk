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

        public double WindPhase { get; private set; }
        public double WindStrength { get; private set; }

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

            WindPhase += dt * 1.65;
            WindStrength = (Math.Sin(WindPhase * 0.45) + 1.0) * 0.85;

            Droplets.Update(dt, IsCompleted, CompletedSegments, rng);

            if (VibrationAmplitude > 0.05)
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
                if (DistanceToSegmentSquared(cursorPos, segment.Start, segment.End) < 100.0)
                {
                    if (cursorSpeed > 1350.0)
                    {
                        DamageNear(cursorPos, 28.0, maxBrokenSegments: 2);
                    }
                    else
                    {
                        double strength = Math.Clamp(cursorSpeed * 0.012, 2.5, 7.5);
                        TriggerVibration(cursorPos, strength);
                        RecentCursorPluckPoint = cursorPos;
                    }

                    _pluckCooldown = 0.25;
                    break;
                }
            }
        }

        public bool CanCatchPoint(Point point)
        {
            if (!IsCompleted) return false;

            Vector toCenter = point - Center;
            if (toCenter.LengthSquared > Radius * Radius * 1.35) return false;

            foreach (var segment in CompletedSegments)
            {
                if (DistanceToSegmentSquared(point, segment.Start, segment.End) < 196.0)
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
