using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin seçilen bölgeye gidip adım adım dairesel veya köşe yelpaze ağı ördüğü durum.
    /// Ağ yeri seçimi WebSitePlanner sınıfına devredilmiştir.
    /// </summary>
    public class SpinningWebState : ISpiderBehaviorState
    {
        private readonly SpiderWebEntity _web;
        private bool _reachedSite;
        private bool _hasCurrentStep;
        private WebBuildStep _currentStep;

        public bool HasReachedSite => _reachedSite;
        public string DisplayName => _reachedSite ? "Ağ Örüyor" : "Ağ Yeri Seçti";

        public bool CanBeStartledByCursor => false;
        public bool CanHuntPrey => !_reachedSite;
        public bool CanEatFromPantry => false;

        public SpinningWebState(SpiderWebEntity web)
        {
            _web = web;
        }

        public static SpinningWebState CreateForBestSite(WorldContext world)
        {
            SpiderWebEntity web = WebSitePlanner.CreateNextWeb(world);
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
                if (spider.MoveTowards(_web.Center, SpiderTuning.ApproachWebSiteSpeed, dt))
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

            double buildSpeed = _currentStep.IsAnchor ? SpiderTuning.SpinAnchorSpeed : SpiderTuning.SpinSpiralSpeed;
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
    }
}
