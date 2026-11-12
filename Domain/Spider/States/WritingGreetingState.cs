using System;
using System.Collections.Generic;
using System.Windows;
using SpiderDesk.Domain.Greeting;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin açılışta, daha hiç ağ örmeden boş masaüstüne ipekle selam yazdığı durum.
    /// Yazının altına gelir, arkasını (ipek memesini) ilk harfe çevirip ipi fırlatır, sonra ipi hiç koparmadan
    /// ipin ucuyla bütün yazıyı yazar. Bitince eserine dönüp el sallar ve ilk ağını örmeye gider.
    /// </summary>
    public class WritingGreetingState : ISpiderBehaviorState
    {
        private enum Phase { Approach, Aim, Launch, Write, Admire }

        private readonly record struct PathPoint(Point Position, bool LaysSilk);

        private const int WavingLegIndex = 1; // Sağ ön bacak

        private readonly string _text;
        private readonly List<PathPoint> _path = new();
        private SilkWriting _writing = null!;
        private Point _standPoint;
        private Point _textCenter;
        private double _strokeSpeed;

        private Phase _phase;
        private double _phaseTime;

        private Point _launchFrom;
        private Point _launchControl;
        private double _launchDuration;

        private Point _tip;
        private int _nextPathIndex;

        public string DisplayName => "Selam Yazıyor";
        public bool CanBeStartledByCursor => false;
        public bool CanHuntPrey => false;
        public bool CanEatFromPantry => false;

        public WritingGreetingState(string text)
        {
            _text = text;
        }

        public void Enter(SpiderEntity spider, WorldContext world)
        {
            _textCenter = new Point(world.ScreenWidth / 2.0, world.ScreenHeight * SpiderTuning.GreetingTextCenterYRatio);
            SilkTextPath layout = SilkTextLayout.Layout(
                _text,
                _textCenter,
                world.ScreenWidth * SpiderTuning.GreetingTextWidthRatio,
                SpiderTuning.GreetingMaxLetterHeight);

            _path.Clear();
            foreach (var stroke in layout.Strokes)
            {
                _path.Add(new PathPoint(stroke[0], LaysSilk: false));
                for (int i = 1; i < stroke.Count; i++)
                {
                    _path.Add(new PathPoint(stroke[i], LaysSilk: true));
                }
            }

            double standY = layout.Bounds.Bottom + layout.LetterHeight * SpiderTuning.GreetingStandOffsetLetters;
            _standPoint = new Point(_textCenter.X, Math.Min(standY, world.ScreenHeight - SpiderTuning.GreetingStandBottomMargin));

            // Uzun selamlar da kısa sürede bitsin diye yazma hızı toplam çizgi uzunluğuna göre ayarlanır
            _strokeSpeed = Math.Max(SpiderTuning.GreetingMinStrokeSpeed, layout.TotalStrokeLength / SpiderTuning.GreetingTargetWriteSeconds);

            _writing = new SilkWriting();
            world.Greeting = _writing;

            spider.ActiveSilkLine = null;
            spider.DraglineAnchor = null;
            SetPhase(Phase.Approach);
        }

        public void Update(SpiderEntity spider, WorldContext world, double dt)
        {
            if (_path.Count == 0)
            {
                Finish(spider, world);
                return;
            }

            _phaseTime += dt;

            switch (_phase)
            {
                case Phase.Approach:
                    if (spider.MoveTowards(_standPoint, SpiderTuning.GreetingApproachSpeed, dt))
                    {
                        SetPhase(Phase.Aim);
                    }
                    break;

                case Phase.Aim:
                    double aimError = AimSpinneretAt(spider, _path[0].Position, SpiderTuning.GreetingAimTurnSpeed, dt);
                    if (aimError < SpiderTuning.GreetingAimTolerance || _phaseTime > SpiderTuning.GreetingAimTimeoutSeconds)
                    {
                        StartLaunch(spider);
                    }
                    break;

                case Phase.Launch:
                    UpdateLaunch(spider);
                    break;

                case Phase.Write:
                    UpdateWrite(spider, world, dt);
                    break;

                case Phase.Admire:
                    UpdateAdmire(spider, world, dt);
                    break;
            }
        }

        public void Exit(SpiderEntity spider, WorldContext world)
        {
            spider.Legs[WavingLegIndex].GestureFoot = null;
            _writing.BeginFade();
        }

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            _phaseTime = 0;
        }

        /// <summary>İlk harfe doğru havada yay çizerek uçan ipi başlatır.</summary>
        private void StartLaunch(SpiderEntity spider)
        {
            Point target = _path[0].Position;
            _launchFrom = spider.GetSpinneretPosition();
            double distance = (target - _launchFrom).Length;

            _launchControl = new Point(
                (_launchFrom.X + target.X) / 2.0,
                (_launchFrom.Y + target.Y) / 2.0 - distance * SpiderTuning.GreetingLaunchArcRatio);
            _launchDuration = SpiderTuning.GreetingLaunchBaseSeconds + distance * SpiderTuning.GreetingLaunchSecondsPerPixel;

            SetPhase(Phase.Launch);
        }

        private void UpdateLaunch(SpiderEntity spider)
        {
            double progress = Math.Min(1.0, _phaseTime / _launchDuration);
            double eased = 1.0 - (1.0 - progress) * (1.0 - progress);
            Point target = _path[0].Position;

            _writing.UpdateStream(spider.GetSpinneretPosition(), WebSegmentGeometry.EvaluateQuadraticBezier(_launchFrom, _launchControl, target, eased),
                isFlowing: false, strokeInProgressStart: null);

            if (progress >= 1.0)
            {
                _writing.TieKnot(target, isLaunchPoint: true);
                _tip = target;
                _nextPathIndex = 1;
                SetPhase(Phase.Write);
            }
        }

        /// <summary>
        /// İpin ucu yazının yolunda sabit hızla ilerler; harf çizgilerinde arkasında tel bırakır,
        /// harfler arasındaki atlamalarda iz bırakmadan daha hızlı geçer. Örümcek ipin ucunu gövdesiyle takip eder.
        /// </summary>
        private void UpdateWrite(SpiderEntity spider, WorldContext world, double dt)
        {
            double speed = _path[_nextPathIndex].LaysSilk ? _strokeSpeed : _strokeSpeed * SpiderTuning.GreetingHopSpeedFactor;
            double remainingMove = speed * dt;

            while (remainingMove > 0 && _nextPathIndex < _path.Count)
            {
                PathPoint next = _path[_nextPathIndex];
                Vector toNext = next.Position - _tip;
                double distance = toNext.Length;

                if (distance <= remainingMove)
                {
                    _tip = next.Position;
                    remainingMove -= distance;
                    if (next.LaysSilk)
                    {
                        _writing.LayThread(_path[_nextPathIndex - 1].Position, next.Position, world.Random);
                    }
                    _writing.TieKnot(next.Position, isLaunchPoint: !next.LaysSilk);
                    _nextPathIndex++;
                }
                else
                {
                    _tip += toNext / distance * remainingMove;
                    remainingMove = 0;
                }
            }

            AimSpinneretAt(spider, _tip, SpiderTuning.GreetingTrackTurnSpeed, dt);

            if (_nextPathIndex >= _path.Count)
            {
                _writing.SnapStream();
                SetPhase(Phase.Admire);
                return;
            }

            Point? strokeStart = _path[_nextPathIndex].LaysSilk ? _path[_nextPathIndex - 1].Position : null;
            _writing.UpdateStream(spider.GetSpinneretPosition(), _tip, isFlowing: true, strokeStart);
        }

        /// <summary>Örümcek yazıya dönüp sağ ön bacağıyla el sallar, ardından ilk ağını örmeye gider.</summary>
        private void UpdateAdmire(SpiderEntity spider, WorldContext world, double dt)
        {
            Vector toText = _textCenter - spider.Position;
            spider.RotateTowards(Math.Atan2(toText.Y, toText.X), SpiderTuning.GreetingAdmireTurnSpeed, dt);

            SpiderLeg wavingLeg = spider.Legs[WavingLegIndex];
            bool isWaving = _phaseTime >= SpiderTuning.GreetingWaveStartSeconds && _phaseTime < SpiderTuning.GreetingWaveEndSeconds;
            if (isWaving)
            {
                double swing = Math.Sin((_phaseTime - SpiderTuning.GreetingWaveStartSeconds) * SpiderTuning.GreetingWaveFrequency);
                wavingLeg.GestureFoot = spider.LocalToWorld(new Vector(30 + 3 * swing, wavingLeg.Side * (14 + 9 * swing)));
            }
            else
            {
                wavingLeg.GestureFoot = null;
            }

            if (_phaseTime >= SpiderTuning.GreetingAdmireSeconds)
            {
                Finish(spider, world);
            }
        }

        private static void Finish(SpiderEntity spider, WorldContext world) =>
            spider.TransitionTo(SpinningWebState.CreateForBestSite(world), world);

        /// <summary>Örümceği, arkası (ipek memesi) hedefe bakacak şekilde döndürür ve kalan açı farkını döner.</summary>
        private static double AimSpinneretAt(SpiderEntity spider, Point target, double turnSpeed, double dt)
        {
            Vector toTarget = target - spider.Position;
            double desiredAngle = Math.Atan2(toTarget.Y, toTarget.X) + Math.PI;
            spider.RotateTowards(desiredAngle, turnSpeed, dt);
            return Math.Abs(Math.IEEERemainder(desiredAngle - spider.Angle, Math.PI * 2.0));
        }
    }
}
