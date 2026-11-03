using System.Linq;
using System.Windows;
using SpiderDesk.Core;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin seçilen bölgeye gidip adım adım ağ ördüğü durum.
    /// </summary>
    public class SpinningWebState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private bool _reachedSite;
        private bool _hasCurrentStep;
        private WebBuildStep _currentStep;

        public string DisplayName => _reachedSite ? "Ağ Örüyor" : "Ağ Yeri Seçti";

        public SpinningWebState(SpiderWebEntity web)
        {
            _web = web;
        }

        public static SpinningWebState CreateForBestSite(WorldContext world)
        {
            Point site = PickBestWebSite(world);
            double radius = 105.0 + world.Random.NextDouble() * 35.0;
            var plan = WebBlueprintGenerator.CreatePlan(site, radius, world.Random);
            var web = new SpiderWebEntity(site, radius, plan);

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
                    _currentStep = _web.BuildQueue.Dequeue();
                    _hasCurrentStep = true;
                }
                else
                {
                    spider.ActiveSilkLine = null;

                    // Sırada bekleyen yeni ağ örme isteği varsa, bu ağ biter bitmez yenisine geç
                    if (spider.TryConsumePendingWebRequest())
                    {
                        spider.TransitionTo(CreateForBestSite(world), world);
                    }
                    else
                    {
                        spider.TransitionTo(new WaitingOnWebState(_web), world);
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
                if (_currentStep.DrawSilk)
                {
                    _web.CompletedSegments.Add(new WebSegment(_currentStep.From, _currentStep.To, _currentStep.IsAnchor));
                }
                _hasCurrentStep = false;
                spider.ActiveSilkLine = null;
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
        }

        private static Point PickBestWebSite(WorldContext world)
        {
            var rng = world.Random;
            const double pad = 155;
            Point[] candidates =
            {
                new Point(pad, pad),
                new Point(world.ScreenWidth - pad, pad),
                new Point(world.ScreenWidth - pad, world.ScreenHeight - pad - 45),
                new Point(pad, world.ScreenHeight - pad - 45),
                new Point(world.ScreenWidth * 0.5, pad),
                new Point(world.ScreenWidth * (0.25 + rng.NextDouble() * 0.5), world.ScreenHeight * (0.25 + rng.NextDouble() * 0.45))
            };

            Point best = candidates[rng.Next(candidates.Length)];
            double bestMinDist = -1;

            foreach (var cand in candidates.OrderBy(_ => rng.Next()))
            {
                Point jittered = new Point(
                    cand.X + (rng.NextDouble() - 0.5) * 60,
                    cand.Y + (rng.NextDouble() - 0.5) * 60
                );

                double minDist = world.Webs.Count == 0
                    ? double.MaxValue
                    : world.Webs.Min(w => (w.Center - jittered).Length);

                if (minDist > bestMinDist)
                {
                    bestMinDist = minDist;
                    best = jittered;
                }
            }

            return best;
        }
    }
}
