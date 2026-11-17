using System;
using System.Windows;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Spider.States;
using SpiderDesk.Domain.Spider.Themes;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Örümceğin fiziksel konumunu, hareketini, seviyesini ve aktif davranış durumunu yönetir.
    /// Durum geçiş izinleri ISpiderBehaviorState üzerinden okunur. Yardımcı işler ayrı sınıflardadır:
    /// av ve kiler arama PreySearch'te, bekleyen mesajlar SpiderMessageQueue'da, otomatik ağ takvimi AutoWebScheduler'da.
    /// </summary>
    public class SpiderEntity
    {
        private readonly SpiderGaitController _gaitController;
        private readonly AutoWebScheduler _autoWebScheduler = new();

        public Point Position { get; private set; }
        public double Angle { get; private set; }
        public Vector VelocityVector { get; private set; }
        public double CurrentSpeed => VelocityVector.Length;

        public SpiderLeg[] Legs { get; }
        public ISpiderBehaviorState CurrentState { get; private set; }
        public ISpiderTheme Theme { get; set; } = SpiderThemeCatalog.All[0];
        public SpiderMetabolism Metabolism { get; } = new();

        public SpiderWebEntity? ActiveWeb { get; set; }
        public (Point Start, Point End)? ActiveSilkLine { get; set; }
        public Point? DraglineAnchor { get; set; }
        public double? CarriedCocoonSize { get; set; }
        public CarriedNote? CarriedNote { get; set; }

        public int BugsCaught { get; private set; }
        public int Level => 1 + (BugsCaught / SpiderTuning.BugsPerLevel);
        public double SpeedMultiplier { get; set; } = 1.0;
        public int PendingWebRequests { get; private set; }

        /// <summary>Örümceğin işini bitirince göstereceği mesajlar (FOCUS/BREAK yazısı, hatırlatma notları).</summary>
        public SpiderMessageQueue Messages { get; } = new();

        public bool HasVisualMotion
        {
            get
            {
                if (CurrentSpeed > 0.05 || ActiveSilkLine != null || DraglineAnchor != null || CurrentState.IsAlwaysAnimating)
                {
                    return true;
                }

                for (int i = 0; i < Legs.Length; i++)
                {
                    if (Legs[i].IsStepping) return true;
                }

                return false;
            }
        }

        public event Action<int>? PreyCaught;

        public SpiderEntity(Point startPosition, SpiderGaitController gaitController, int initialBugsCaught = 0)
        {
            Position = startPosition;
            Angle = -Math.PI / 4.0;
            BugsCaught = initialBugsCaught;
            _gaitController = gaitController;
            Legs = _gaitController.CreateLegs(this);
            CurrentState = new WanderingState();
        }

        public void RecordPreyCaught()
        {
            BugsCaught++;
            PreyCaught?.Invoke(BugsCaught);
        }

        public void RequestNewWeb(WorldContext world)
        {
            if (CurrentState is SpinningWebState || (ActiveWeb != null && !ActiveWeb.IsCompleted))
            {
                PendingWebRequests++;
                return;
            }

            TransitionTo(SpinningWebState.CreateForBestSite(world), world);
        }

        public bool TryConsumePendingWebRequest()
        {
            if (PendingWebRequests > 0)
            {
                PendingWebRequests--;
                return true;
            }
            return false;
        }

        public void ClearPendingWebRequests()
        {
            PendingWebRequests = 0;
            _autoWebScheduler.Reset();
        }

        public void TransitionTo(ISpiderBehaviorState nextState, WorldContext world)
        {
            CurrentState.Exit(this, world);
            CurrentState = nextState;
            CurrentState.Enter(this, world);
        }

        public void RequestRappel(WorldContext world)
        {
            if (!CurrentState.CanBeStartledByCursor) return;
            TransitionTo(new RappellingState(), world);
        }

        public void Update(WorldContext world, double dt)
        {
            VelocityVector = new Vector(0, 0);

            // Örümcek işini bırakabiliyorsa (ağ örmüyor, av taşımıyorsa) sırada bekleyen mesajı göstermeye geçer.
            // Bırakamıyorsa TakeNextMessageState hiç çağrılmaz; mesaj sırada kalır.
            if (CurrentState.CanStopForMessage && Messages.TakeNextMessageState() is { } messageState)
            {
                TransitionTo(messageState, world);
            }
            else if (ShouldFleeFromCursor(world))
            {
                TransitionTo(new StartledByMouseState(CurrentState), world);
            }
            else
            {
                BugEntity? targetBug = CurrentState.CanHuntPrey ? PreySearch.FindPrey(this, world) : null;
                if (targetBug != null)
                {
                    TransitionTo(new HuntingState(targetBug, CurrentState), world);
                }
                else if (Metabolism.IsHungry && CurrentState.CanEatFromPantry)
                {
                    SpiderWebEntity? webWithFood = PreySearch.FindWebWithStoredFood(this, world);
                    if (webWithFood?.Pantry.PeekNextMeal() is { } storedCocoon)
                    {
                        TransitionTo(new EatingFromPantryState(webWithFood, storedCocoon), world);
                    }
                }
            }

            CurrentState.Update(this, world, dt);
            _autoWebScheduler.Update(this, world, dt);
            Metabolism.Update(dt, CurrentSpeed);
            _gaitController.UpdateGait(this, dt);
        }

        private bool ShouldFleeFromCursor(WorldContext world)
        {
            if (!CurrentState.CanBeStartledByCursor)
            {
                return false;
            }

            double distSq = (world.CursorPosition - Position).LengthSquared;
            const double startleRadiusSq = SpiderTuning.CursorStartleDistance * SpiderTuning.CursorStartleDistance;
            return distSq < startleRadiusSq && world.CursorSpeed > SpiderTuning.CursorStartleMinSpeed;
        }

        public bool MoveTowards(Point target, double speed, double dt, double stopDistance = SpiderTuning.DefaultStopDistance)
        {
            Vector toTarget = target - Position;
            double dist = toTarget.Length;

            if (dist <= stopDistance)
            {
                Position = target;
                VelocityVector = new Vector(0, 0);
                return true;
            }

            double effectiveSpeed = speed * SpeedMultiplier;
            double desiredAngle = Math.Atan2(toTarget.Y, toTarget.X);
            RotateTowards(desiredAngle, SpiderTuning.DefaultTurnSpeed * SpeedMultiplier, dt);

            Vector dir = toTarget / dist;
            double step = Math.Min(dist, effectiveSpeed * dt);
            Position += dir * step;
            VelocityVector = dir * (step / Math.Max(dt, 0.001));
            return false;
        }

        public void RotateBy(double deltaAngle)
        {
            Angle = NormalizeAngle(Angle + deltaAngle);
        }

        public void RotateTowards(double targetAngle, double turnSpeed, double dt)
        {
            double diff = NormalizeAngle(targetAngle - Angle);
            Angle = NormalizeAngle(Angle + diff * Math.Min(1.0, turnSpeed * dt));
        }

        public Point LocalToWorld(Vector local)
        {
            double cos = Math.Cos(Angle);
            double sin = Math.Sin(Angle);
            return new Point(
                Position.X + local.X * cos - local.Y * sin,
                Position.Y + local.X * sin + local.Y * cos
            );
        }

        public Point GetSpinneretPosition() => LocalToWorld(new Vector(-18, 0));

        private static double NormalizeAngle(double angle)
        {
            while (angle > Math.PI) angle -= Math.PI * 2;
            while (angle < -Math.PI) angle += Math.PI * 2;
            return angle;
        }
    }
}
