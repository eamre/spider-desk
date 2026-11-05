using System.Linq;
using System.Windows;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin masaüstünde serbestçe gezindiği ve yeni ağ kuracak yer aradığı durum.
    /// </summary>
    public class WanderingState : ISpiderBehaviorState
    {
        private Point _targetPoint;
        private double _timer;

        public string DisplayName => "Geziniyor";
        public bool CanStartScheduledWeb => true;
        public bool IsAlwaysAnimating => false;

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = spider.Position;
            PickNextDestination(spider, world);
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            _timer -= dt;

            // Gezinirken herhangi bir ağda hasar oluşursa hemen gidip tamir et!
            for (int i = 0; i < world.Webs.Count; i++)
            {
                var web = world.Webs[i];
                if (web.IsCompleted && web.HasDamage)
                {
                    spider.ActiveWeb = web;
                    spider.TransitionTo(new RepairingWebState(web), world);
                    return;
                }
            }

            if (world.Webs.Count == 0 && _timer <= 0)
            {
                spider.TransitionTo(SpinningWebState.CreateForBestSite(world), world);
                return;
            }

            if (spider.MoveTowards(_targetPoint, SpiderTuning.WanderMoveSpeed, dt) || _timer <= 0)
            {
                var completedWebs = world.Webs.Where(w => w.IsCompleted).ToList();
                if (completedWebs.Count > 0 && world.Random.NextDouble() < 0.60)
                {
                    var web = completedWebs[world.Random.Next(completedWebs.Count)];
                    spider.ActiveWeb = web;
                    spider.TransitionTo(new WaitingOnWebState(web), world);
                }
                else
                {
                    PickNextDestination(spider, world);
                }
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            spider.DraglineAnchor = null;
        }

        private void PickNextDestination(SpiderEntity spider, WorldContext world)
        {
            _targetPoint = new Point(
                100 + world.Random.NextDouble() * (world.ScreenWidth - 200),
                100 + world.Random.NextDouble() * (world.ScreenHeight - 220)
            );
            spider.DraglineAnchor = spider.Position;
            _timer = 3.5 + world.Random.NextDouble() * 4.0;
        }
    }
}
