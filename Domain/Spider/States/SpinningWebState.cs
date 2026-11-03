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
                double radius = 190.0 + world.Random.NextDouble() * 35.0;
                double midAngle = (candidate.StartAngle + candidate.EndAngle) * 0.5;
                Point restingHub = WebBlueprintGenerator.PolarToCartesian(candidate.Vertex, midAngle, 48.0);

                var plan = WebBlueprintGenerator.CreateCornerFanPlan(
                    candidate.Vertex,
                    restingHub,
                    radius,
                    candidate.StartAngle,
                    candidate.EndAngle,
                    world.Random
                );

                web = new SpiderWebEntity(
                    candidate.Vertex,
                    restingHub,
                    radius,
                    plan,
                    candidate.StartAngle,
                    candidate.EndAngle
                );
            }
            else
            {
                double radius = 105.0 + world.Random.NextDouble() * 35.0;
                var plan = WebBlueprintGenerator.CreateOrbPlan(candidate.Vertex, radius, world.Random);
                web = new SpiderWebEntity(candidate.Vertex, candidate.Vertex, radius, plan);
            }

            web.Droplets.SpawnRate = world.DewSpawnRate;
            world.Webs.Add(web);

            if (world.Webs.Count > 4)
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

            const double cornerMargin = 14.0;
            const double taskbarPad = 56.0;
            const double orbPad = 165.0;

            WebSiteCandidate[] candidates =
            {
                // 4 Ekran Köşesi (90 derecelik Köşe Yelpaze Ağları)
                new(new Point(cornerMargin, cornerMargin), true, 0.0, Math.PI * 0.5),
                new(new Point(w - cornerMargin, cornerMargin), true, Math.PI * 0.5, Math.PI),
                new(new Point(w - cornerMargin, h - taskbarPad), true, Math.PI, Math.PI * 1.5),
                new(new Point(cornerMargin, h - taskbarPad), true, Math.PI * 1.5, Math.PI * 2.0),

                // Masaüstü İç Bölgeleri (Tam Dairesel Ağlar)
                new(new Point(
                    orbPad + rng.NextDouble() * (w - orbPad * 2),
                    orbPad + rng.NextDouble() * (h - orbPad * 2 - 40)),
                    false, 0.0, Math.PI * 2.0),
                new(new Point(
                    w * (0.28 + rng.NextDouble() * 0.44),
                    h * (0.25 + rng.NextDouble() * 0.45)),
                    false, 0.0, Math.PI * 2.0)
            };

            WebSiteCandidate best = candidates[rng.Next(candidates.Length)];
            double bestMinDist = -1;

            foreach (var cand in candidates.OrderBy(_ => rng.Next()))
            {
                double minDist = world.Webs.Count == 0
                    ? double.MaxValue
                    : world.Webs.Min(existing => (existing.Center - cand.Vertex).Length);

                if (minDist > bestMinDist)
                {
                    bestMinDist = minDist;
                    best = cand;
                }
            }

            return best;
        }
    }
}
