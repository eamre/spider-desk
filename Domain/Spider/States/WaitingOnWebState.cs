using System;
using System.Windows;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin tamamladığı ağın nöbet merkezinde veya telleri üzerinde nöbet tuttuğu durum.
    /// </summary>
    public class WaitingOnWebState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private Point _patrolTarget;
        private double _patrolTimer;
        private bool _reachedHub;

        public string DisplayName => _reachedHub ? "Ağda Nöbette" : "Ağa Gidiyor";
        public bool CanStartScheduledWeb => true;
        public bool IsAlwaysAnimating => false;

        public WaitingOnWebState(SpiderWebEntity web)
        {
            _web = web;
            _patrolTarget = web.HubPosition;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveWeb = _web;
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
            _patrolTarget = _web.HubPosition;
            _reachedHub = (spider.Position - _web.HubPosition).Length < 25.0;
            _patrolTimer = 10.0 + world.Random.NextDouble() * 8.0;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            // 1. Bulunduğu ağda kopuk/hasarlı tel varsa öncelikle gidip onu onar!
            if (_web.HasDamage)
            {
                spider.TransitionTo(new RepairingWebState(_web), world);
                return;
            }

            // 1.1. Bulunduğu ağ sağlamsa, dünyadaki diğer tamamlanmış ağlarda hasar var mı kontrol et!
            // Hasarlı bir ağ varsa hemen oraya yönelip tamire başla.
            SpiderWebEntity? nearestDamagedWeb = null;
            double bestDamagedDistSq = double.MaxValue;
            for (int i = 0; i < world.Webs.Count; i++)
            {
                var otherWeb = world.Webs[i];
                if (otherWeb != _web && otherWeb.IsCompleted && otherWeb.HasDamage)
                {
                    double distSq = (otherWeb.Center - spider.Position).LengthSquared;
                    if (distSq < bestDamagedDistSq)
                    {
                        bestDamagedDistSq = distSq;
                        nearestDamagedWeb = otherWeb;
                    }
                }
            }

            if (nearestDamagedWeb != null)
            {
                spider.ActiveWeb = nearestDamagedWeb;
                spider.TransitionTo(new RepairingWebState(nearestDamagedWeb), world);
                return;
            }

            // 2. Ağda çok fazla çiy/su damlası biriktiyse önce ağın nöbet merkezine yürü,
            // SADECE merkeze vardığında silkeleme durumuna geç!
            if (_web.Droplets.IsHeavyWithWater)
            {
                _patrolTarget = _web.HubPosition;
                if (spider.MoveTowards(_web.HubPosition, SpiderTuning.ApproachHubSpeed, dt, stopDistance: 2.5))
                {
                    spider.TransitionTo(new ShakingWebState(_web), world);
                }
                return;
            }

            // 3. Kullanıcı fareyle ağın tellerini titretti mi? Hemen kontrol etmeye git!
            if (_web.RecentCursorPluckPoint is { } pluckPoint)
            {
                _web.RecentCursorPluckPoint = null;
                spider.TransitionTo(new InvestigatingVibrationState(_web, pluckPoint), world);
                return;
            }

            // 4. Henüz ağın merkezine varmadıysa, önce merkezine doğru yürü
            if (!_reachedHub)
            {
                if (spider.MoveTowards(_web.HubPosition, SpiderTuning.ApproachHubSpeed, dt, stopDistance: SpiderTuning.HubApproachStopDistance))
                {
                    _reachedHub = true;
                    _patrolTimer = SpiderTuning.WebPatrolInitialDurationBase + world.Random.NextDouble() * SpiderTuning.WebPatrolInitialDurationVariance;
                }
                return;
            }

            spider.MoveTowards(_patrolTarget, SpiderTuning.PatrolMoveSpeed, dt);
            _patrolTimer -= dt;

            if (_patrolTimer <= 0)
            {
                bool hasTrappedOnCurrentWeb = false;
                for (int i = 0; i < world.Bugs.Count; i++)
                {
                    if (world.Bugs[i].CurrentState.Status == BugLifecycleStatus.TrappedInWeb && world.Bugs[i].TrappedWeb == _web)
                    {
                        hasTrappedOnCurrentWeb = true;
                        break;
                    }
                }

                if (!hasTrappedOnCurrentWeb)
                {
                    var otherCompletedWebs = world.Webs.Where(w => w.IsCompleted && w != _web).ToList();
                    double roll = world.Random.NextDouble();

                    if (otherCompletedWebs.Count > 0 && roll < SpiderTuning.WebRotationChance)
                    {
                        // Diğer bir ağa nöbet rotasyonuna / kontrole git!
                        var nextWeb = otherCompletedWebs[world.Random.Next(otherCompletedWebs.Count)];
                        spider.ActiveWeb = nextWeb;
                        spider.TransitionTo(new WaitingOnWebState(nextWeb), world);
                        return;
                    }
                    else if (roll < (otherCompletedWebs.Count > 0 ? 0.60 : 0.16))
                    {
                        spider.TransitionTo(new RappellingState(), world);
                        return;
                    }
                    else if (roll < (otherCompletedWebs.Count > 0 ? 0.75 : 0.36))
                    {
                        spider.TransitionTo(new WanderingState(), world);
                        return;
                    }
                }

                // Bu ağ üzerinde devriye gezmeye devam et
                double span = _web.EndAngle - _web.StartAngle;
                double angle = _web.StartAngle + span * (0.15 + world.Random.NextDouble() * 0.70);
                double dist = (SpiderTuning.WebPatrolRadiusMinRatio + world.Random.NextDouble() * SpiderTuning.WebPatrolRadiusSpanRatio) * _web.Radius;

                _patrolTarget = new Point(
                    _web.Center.X + Math.Cos(angle) * dist,
                    _web.Center.Y + Math.Sin(angle) * dist
                );
                _patrolTimer = SpiderTuning.WebPatrolSubsequentDurationBase + world.Random.NextDouble() * SpiderTuning.WebPatrolSubsequentDurationVariance;
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
        }
    }
}
