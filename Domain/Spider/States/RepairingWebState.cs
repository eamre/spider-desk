using SpiderDesk.Core;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Ağın kopan/hasar gören tellerini tek tek yeniden örerek onardığı durum (OCP & SRP).
    /// </summary>
    public class RepairingWebState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private WebSegment _targetSegment;
        private bool _hasSegment;
        private bool _atSegmentStart;

        public string DisplayName => "Ağı Tamir Ediyor";

        public RepairingWebState(SpiderWebEntity web)
        {
            _web = web;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveWeb = _web;
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            if (!_hasSegment)
            {
                if (_web.DamagedSegments.Count > 0)
                {
                    _targetSegment = _web.DamagedSegments.Dequeue();
                    _hasSegment = true;
                    _atSegmentStart = false;
                }
                else
                {
                    spider.ActiveSilkLine = null;
                    spider.TransitionTo(new WaitingOnWebState(_web), world);
                    return;
                }
            }

            // 1. Önce kopuk telin başlangıç düğümüne yürü
            if (!_atSegmentStart)
            {
                spider.ActiveSilkLine = null;
                if (spider.MoveTowards(_targetSegment.Start, 185.0, dt))
                {
                    _atSegmentStart = true;
                }
                return;
            }

            // 2. Başlangıç düğümünden bitiş düğümüne ip çekerek yürü ve teli onar
            spider.ActiveSilkLine = (_targetSegment.Start, spider.GetSpinneretPosition());

            if (spider.MoveTowards(_targetSegment.End, 200.0, dt))
            {
                _web.CompletedSegments.Add(_targetSegment);
                spider.ActiveSilkLine = null;
                _hasSegment = false;
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            // Eğer avlanma vb. sebeple tamir yarıda kesildiyse elindeki teli tekrar tamir kuyruğuna koy
            if (_hasSegment)
            {
                _web.DamagedSegments.Enqueue(_targetSegment);
                _hasSegment = false;
            }
            spider.ActiveSilkLine = null;
        }
    }
}
