using System;
using System.Linq;
using System.Windows;
using SpiderDesk.Core;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin seçilen bölgeye gidip adım adım dairesel veya köşe yelpaze ağı ördüğü durum.
    /// </summary>
    public class SpinningWebState : ISpiderBehaviorState
    {
        private readonly record struct WebSiteCandidate(
            Point Vertex,
            double Radius,
            bool IsCornerFan,
            double StartAngle,
            double EndAngle);

        private readonly SpiderWebEntity _web;
        private bool _reachedSite;
        private bool _hasCurrentStep;
        private WebBuildStep _currentStep;

        public bool HasReachedSite => _reachedSite;
        public string DisplayName => _reachedSite ? "Ağ Örüyor" : "Ağ Yeri Seçti";

        public SpinningWebState(SpiderWebEntity web)
        {
            _web = web;
        }

        public static SpinningWebState CreateForBestSite(WorldContext world)
        {
            WebSiteCandidate candidate = PickBestWebSite(world);
            SpiderWebEntity web;

            if (candidate.IsCornerFan)
            {
                double midAngle = (candidate.StartAngle + candidate.EndAngle) * 0.5;
                Point restingHub = WebBlueprintGenerator.PolarToCartesian(candidate.Vertex, midAngle, 58.0);

                var plan = WebBlueprintGenerator.CreateCornerFanPlan(
                    candidate.Vertex,
                    restingHub,
                    candidate.Radius,
                    candidate.StartAngle,
                    candidate.EndAngle,
                    world.Random
                );

                web = new SpiderWebEntity(
                    candidate.Vertex,
                    restingHub,
                    candidate.Radius,
                    plan,
                    candidate.StartAngle,
                    candidate.EndAngle
                );
            }
            else
            {
                var plan = WebBlueprintGenerator.CreateOrbPlan(candidate.Vertex, candidate.Radius, world.Random);
                web = new SpiderWebEntity(candidate.Vertex, candidate.Vertex, candidate.Radius, plan);
            }

            web.Droplets.SpawnRate = world.DewSpawnRate;
            world.Webs.Add(web);

            if (world.Webs.Count > 6)
            {
                world.Webs.RemoveAt(0);
            }

            return new SpinningWebState(web);
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveWeb = _web;
            if (!_reachedSite)
            {
                spider.DraglineAnchor = spider.Position;
            }
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            if (!_reachedSite)
            {
                if (spider.MoveTowards(_web.Center, 175.0, dt))
                {
                    _reachedSite = true;
                    spider.DraglineAnchor = null;
                }
                return;
            }

            if (!_hasCurrentStep)
            {
                if (_web.BuildQueue.Count > 0)
                {
                    _currentStep = _web.BuildQueue.Peek();
                    _hasCurrentStep = true;
                    _web.IsStepInProgress = true;
                }
                else
                {
                    _web.IsStepInProgress = false;
                    spider.ActiveSilkLine = null;

                    ISpiderBehaviorState nextState = spider.TryConsumePendingWebRequest()
                        ? CreateForBestSite(world)
                        : new WaitingOnWebState(_web);

                    if (spider.CarriedCocoonSize is { } carriedSize)
                    {
                        spider.CarriedCocoonSize = null;
                        spider.TransitionTo(new StoringPreyState(_web, carriedSize, nextState), world);
                    }
                    else
                    {
                        spider.TransitionTo(nextState, world);
                    }
                    return;
                }
            }

            double buildSpeed = _currentStep.IsAnchor ? 195.0 : 230.0;
            if (_currentStep.DrawSilk)
            {
                spider.ActiveSilkLine = (_currentStep.From, spider.GetSpinneretPosition());
            }
            else
            {
                spider.ActiveSilkLine = null;
            }

            if (spider.MoveTowards(_currentStep.To, buildSpeed, dt))
            {
                if (_web.BuildQueue.Count > 0)
                {
                    _web.BuildQueue.Dequeue();
                }

                if (_currentStep.DrawSilk)
                {
                    _web.CompletedSegments.Add(new WebSegment(_currentStep.From, _currentStep.To, _currentStep.IsAnchor));
                }
                _hasCurrentStep = false;
                _web.IsStepInProgress = false;
                spider.ActiveSilkLine = null;
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            _web.IsStepInProgress = false;
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
        }

        private static WebSiteCandidate PickBestWebSite(WorldContext world)
        {
            var rng = world.Random;
            double w = world.ScreenWidth;
            double h = world.ScreenHeight;

            const double cornerMargin = 3.0;
            const double taskbarPad = 48.0;

            Point[] cornerVertices =
            {
                new(cornerMargin, cornerMargin),
                new(w - cornerMargin, cornerMargin),
                new(w - cornerMargin, h - taskbarPad),
                new(cornerMargin, h - taskbarPad)
            };

            // 1. Köşe Yelpaze Ağı Adayları (Mevcut ağlarla kesişmeyen boş köşeler)
            double cornerRadius = 215.0 + rng.NextDouble() * 30.0;
            WebSiteCandidate[] allCorners =
            {
                new(cornerVertices[0], cornerRadius, true, 0.0, Math.PI * 0.5),
                new(cornerVertices[1], cornerRadius, true, Math.PI * 0.5, Math.PI),
                new(cornerVertices[2], cornerRadius, true, Math.PI, Math.PI * 1.5),
                new(cornerVertices[3], cornerRadius, true, Math.PI * 1.5, Math.PI * 2.0)
            };

            var validCorners = allCorners
                .Where(c => !OverlapsAnyExistingWeb(world, c))
                .OrderBy(_ => rng.Next())
                .ToList();

            // 2. Tam Dairesel (Orb) Ağ Adayları (Mevcut ağlara ve köşe bölgelerine değmeyen iç noktalar)
            WebSiteCandidate? bestOrbCandidate = FindNonOverlappingOrbCandidate(world, cornerVertices, rng);

            // 3. Köşe / Orta Dengesi:
            // Ekrandaki tüm ağlar köşeyse sıradaki orta olsun; tümü ortaysa sıradaki köşe olsun; aksi halde %50-%50 seç.
            bool preferCorner;
            if (world.Webs.Count > 0 && world.Webs.All(existing => existing.IsCornerFan))
            {
                preferCorner = false;
            }
            else if (world.Webs.Count > 0 && world.Webs.All(existing => !existing.IsCornerFan))
            {
                preferCorner = true;
            }
            else
            {
                preferCorner = rng.NextDouble() < 0.50;
            }

            if (preferCorner)
            {
                if (validCorners.Count > 0) return validCorners[0];
                if (bestOrbCandidate is { } orbFallback) return orbFallback;
            }
            else
            {
                if (bestOrbCandidate is { } orb) return orb;
                if (validCorners.Count > 0) return validCorners[0];
            }

            return allCorners[rng.Next(allCorners.Length)];
        }

        private static WebSiteCandidate? FindNonOverlappingOrbCandidate(
            WorldContext world,
            Point[] cornerVertices,
            Random rng)
        {
            double w = world.ScreenWidth;
            double h = world.ScreenHeight;

            // Önce standart yarıçapla (105-130 px), ekran çok doluysa biraz daha kompakt yarıçapla (85-100 px) dene
            double[] radiusAttempts = { 105.0 + rng.NextDouble() * 25.0, 88.0 + rng.NextDouble() * 14.0 };

            foreach (double orbRadius in radiusAttempts)
            {
                double marginX = orbRadius * 1.25 + 25.0;
                double marginY = orbRadius * 1.25 + 55.0;

                WebSiteCandidate? best = null;
                double bestClearance = -1;

                for (int i = 0; i < 45; i++)
                {
                    Point pt = new Point(
                        marginX + rng.NextDouble() * Math.Max(80.0, w - marginX * 2),
                        marginY + rng.NextDouble() * Math.Max(80.0, h - marginY * 2)
                    );

                    // Köşe ağlarının kurulacağı 4 köşeye de çok yaklaşmasın ki sonradan örülecek köşe ağlarına değmesin
                    bool tooCloseToAnyCorner = cornerVertices.Any(cv => (cv - pt).Length < 250.0 + orbRadius * 1.15);
                    if (tooCloseToAnyCorner) continue;

                    var candidate = new WebSiteCandidate(pt, orbRadius, false, 0.0, Math.PI * 2.0);
                    if (OverlapsAnyExistingWeb(world, candidate)) continue;

                    double minClearance = world.Webs.Count == 0
                        ? double.MaxValue
                        : world.Webs.Min(existing => (existing.HubPosition - pt).Length);

                    if (minClearance > bestClearance)
                    {
                        bestClearance = minClearance;
                        best = candidate;
                    }
                }

                if (best != null)
                {
                    return best;
                }
            }

            return null;
        }

        private static bool OverlapsAnyExistingWeb(WorldContext world, WebSiteCandidate candidate)
        {
            const double safetyGap = 38.0;
            double candidateExtent = candidate.IsCornerFan
                ? candidate.Radius * 1.18
                : candidate.Radius * 1.15;

            foreach (var existing in world.Webs)
            {
                double existingExtent = existing.IsCornerFan
                    ? existing.Radius * 1.18
                    : existing.Radius * 1.15;

                double centerDist = (existing.Center - candidate.Vertex).Length;
                if (centerDist < existingExtent + candidateExtent + safetyGap)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
