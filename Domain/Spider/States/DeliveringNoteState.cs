using System;
using System.Windows;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin tavandan baş aşağı sarkıp ön iki bacağıyla bir notu tabela gibi gösterdiği durum (örneğin bir hatırlatma).
    /// Bir süre sallanır, fare hızla yaklaşırsa erken toplanır; yukarı çıkarken not solar ve örümcek evine döner.
    /// </summary>
    public sealed class DeliveringNoteState : ISpiderBehaviorState
    {
        private enum Phase
        {
            ClimbingToCeiling,
            Descending,
            Holding,
            Ascending
        }

        private const double CeilingY = 24;
        private const double LineLengthAtTop = 28;

        private readonly CarriedNote _note;
        private Phase _phase = Phase.ClimbingToCeiling;
        private Point _ceilingAnchor;
        private HangingSilkLine _line = null!;
        private double _targetDropY;
        private double _holdTimer;

        public string Text => _note.Text;

        public string DisplayName => "Not Getiriyor";
        public bool CanBeStartledByCursor => false;
        public bool CanHuntPrey => false;
        public bool CanEatFromPantry => false;

        public DeliveringNoteState(string text)
        {
            _note = new CarriedNote(text);
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            double anchorX = Math.Clamp(spider.Position.X, SpiderTuning.NoteScreenEdgeMargin, world.ScreenWidth - SpiderTuning.NoteScreenEdgeMargin);
            _ceilingAnchor = new Point(anchorX, 2);
            _targetDropY = world.ScreenHeight * SpiderTuning.NoteHangHeightRatio;
            _holdTimer = SpiderTuning.NoteHangSeconds;
            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            switch (_phase)
            {
                case Phase.ClimbingToCeiling:
                    if (spider.MoveTowards(new Point(_ceilingAnchor.X, CeilingY), SpiderTuning.RappelClimbSpeed, dt))
                    {
                        _line = new HangingSilkLine(_ceilingAnchor, length: CeilingY, swayAmplitude: 10.0);
                        spider.CarriedNote = _note;
                        _phase = Phase.Descending;
                    }
                    return;

                case Phase.Descending:
                    _line.Length = Math.Min(_targetDropY, _line.Length + SpiderTuning.RappelDescendSpeed * dt);
                    _line.MoveSpider(spider, dt, hangAlongThread: true);
                    FadeInNoteOnceUpright(spider, dt);

                    if (HangingSilkLine.IsCursorPushing(spider, world))
                    {
                        StartAscending(swayAmplitude: 28.0);
                    }
                    else if (_line.Length >= _targetDropY)
                    {
                        _phase = Phase.Holding;
                    }
                    break;

                case Phase.Holding:
                    _holdTimer -= dt;
                    _line.SwayAmplitude = Math.Max(6.0, _line.SwayAmplitude * Math.Pow(0.65, dt));
                    _line.MoveSpider(spider, dt, hangAlongThread: true);
                    FadeInNoteOnceUpright(spider, dt);

                    // Fare hızla yaklaşırsa "tamam, gördüm" sayılır: örümcek notu toplayıp yukarı kaçar
                    if (HangingSilkLine.IsCursorPushing(spider, world))
                    {
                        StartAscending(swayAmplitude: 32.0);
                    }
                    else if (_holdTimer <= 0)
                    {
                        StartAscending(swayAmplitude: _line.SwayAmplitude);
                    }
                    break;

                case Phase.Ascending:
                    _line.Length -= SpiderTuning.RappelAscendSpeed * dt;
                    // Baş aşağı kalarak ipi toplar; böylece not hep altında ve okunur kalır
                    _line.MoveSpider(spider, dt, hangAlongThread: true);
                    double fadeByHeight = (_line.Length - LineLengthAtTop) / SpiderTuning.NoteFadeOutDistance;
                    _note.Opacity = Math.Clamp(Math.Min(_note.Opacity, fadeByHeight), 0.0, 1.0);

                    if (_line.Length <= LineLengthAtTop)
                    {
                        RappellingState.ReturnHome(spider, world);
                        return;
                    }
                    break;
            }

            HoldNote(spider);
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            spider.ActiveSilkLine = null;
            spider.CarriedNote = null;
            foreach (var leg in spider.Legs)
            {
                if (IsFrontLeg(leg)) leg.GestureFoot = null;
            }
        }

        /// <summary>
        /// Not, örümcek tavanda dönüp baş aşağı oturduktan sonra belirir; dönerken görünseydi kart yamuk dururdu.
        /// </summary>
        private void FadeInNoteOnceUpright(SpiderEntity spider, double dt)
        {
            if (Math.Abs(spider.Angle - Math.PI / 2.0) > SpiderTuning.NoteUprightTolerance) return;
            _note.Opacity = Math.Min(1.0, _note.Opacity + dt / SpiderTuning.NoteFadeInSeconds);
        }

        private void StartAscending(double swayAmplitude)
        {
            _line.SwayAmplitude = swayAmplitude;
            _phase = Phase.Ascending;
        }

        /// <summary>Kartı başın önüne yerleştirir ve ön iki bacağın uçlarını kartın üst kenarının iki yanına koyar.</summary>
        private void HoldNote(SpiderEntity spider)
        {
            _note.TopCenter = spider.LocalToWorld(new Vector(SpiderTuning.NoteHoldDistance, 0));
            // Baş aşağı dururken (açı = 90°) kart diktir; örümcek eğildikçe kart da aynı açıyla eğilir
            _note.Tilt = spider.Angle - Math.PI / 2.0;
            foreach (var leg in spider.Legs)
            {
                if (!IsFrontLeg(leg)) continue;
                leg.GestureFoot = spider.LocalToWorld(new Vector(SpiderTuning.NoteHoldDistance, leg.Side * SpiderTuning.NoteGripHalfSpan));
            }
        }

        private static bool IsFrontLeg(SpiderLeg leg) => leg.PairIndex == 0;
    }
}
