using SpiderDesk.Domain;
using SpiderDesk.Domain.Spider;

namespace SpiderDesk.Infrastructure.Sessions
{
    /// <summary>
    /// "Örümcek" sekmesini (<see cref="ISpiderView"/>) simülasyona bağlar: sekmedeki komutları örümceğe iletir,
    /// örümceğin durumunu da sekmeye yansıtır.
    /// </summary>
    public sealed class SpiderSessionController
    {
        /// <summary>"Böcek gönder"e her basışta ekrana gelen böcek sayısı.</summary>
        public const int BugsPerSpawn = 3;

        private readonly SimulationEngine _engine;
        private readonly ISpiderView _view;

        // Durum her karede sorulur ama yalnızca değişince sekmeye gönderilir; böylece her karede yeni yazı üretilmez
        private (int Bugs, int Satiety, string Activity)? _shown;

        public SpiderSessionController(SimulationEngine engine, ISpiderView view)
        {
            _engine = engine;
            _view = view;

            _view.NewWebRequested += () => _engine.CommandNewWeb();
            _view.RappelRequested += () => _engine.CommandRappel();
            _view.RainRequested += () => _engine.CommandRainOnWebs();
            _view.SpawnBugsRequested += () => _engine.SpawnExtraBugs(BugsPerSpawn);
            _view.ClearWebsRequested += () => _engine.ClearWebs();

            ShowStatus();
        }

        /// <summary>Kare döngüsünden çağrılır; örümceğin durumu değiştiyse sekmeyi günceller.</summary>
        public void ShowStatus()
        {
            SpiderEntity spider = _engine.Spider;
            var current = (spider.BugsCaught, spider.Metabolism.SatietyPercent, spider.CurrentState.DisplayName);
            if (_shown == current) return;

            _shown = current;
            _view.ShowStatus(SpiderStatusDisplay.For(current.BugsCaught, current.SatietyPercent, current.DisplayName));
        }
    }
}
