using SpiderDesk.Core;
using SpiderDesk.Domain.Spider.States;

namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Otomatik ağ üretim takvimini (önce 2 ağ, +5 dk sonra 3. ağ, +10 dk sonra 4. ağ, +30 dk sonra 5. ağ) yönetir.
    /// </summary>
    public class AutoWebScheduler
    {
        public const int MaxAutoWebs = 5;

        private double _timer;
        private int _scheduledForWebCount;

        public void Reset()
        {
            _scheduledForWebCount = 0;
            _timer = 0;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            int webCount = world.Webs.Count;
            if (webCount == 0)
            {
                Reset();
                return;
            }

            if (webCount >= MaxAutoWebs || spider.PendingWebRequests > 0)
            {
                return;
            }

            for (int i = 0; i < webCount; i++)
            {
                if (!world.Webs[i].IsCompleted)
                {
                    return;
                }
            }

            if (_scheduledForWebCount != webCount)
            {
                _scheduledForWebCount = webCount;
                _timer = webCount switch
                {
                    1 => 2.5,      // Önce 2 ağ örülsün: 1. ağ biter bitmez (2.5 sn sonra) 2. ağa başlar
                    2 => 300.0,    // 2. ağdan 5 dakika (300 sn) sonra 3. ağ örülsün
                    3 => 600.0,    // 3. ağdan 10 dakika (600 sn) sonra 4. ağ örülsün
                    4 => 1800.0,   // 4. ağdan 30 dakika (1800 sn) sonra 5. ağ örülsün
                    _ => double.MaxValue
                };
            }

            _timer -= dt;
            if (_timer <= 0 && spider.CurrentState.CanStartScheduledWeb)
            {
                spider.TransitionTo(SpinningWebState.CreateForBestSite(world), world);
            }
        }
    }
}
