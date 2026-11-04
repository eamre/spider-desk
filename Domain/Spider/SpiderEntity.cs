using System;
using System.Linq;
using System.Windows;
using SpiderDesk.Core;
using SpiderDesk.Domain.Insects;
using SpiderDesk.Domain.Spider.States;
using SpiderDesk.Domain.Spider.Themes;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Örümceğin fiziksel konumunu, hareketini, seviyesini ve aktif davranış durumunu (State Pattern) yönetir.
    /// </summary>
    public class SpiderEntity
    {
        private readonly SpiderGaitController _gaitController;

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

        public const int MaxAutoWebs = 5;

        public int BugsCaught { get; private set; }
        public int Level => 1 + (BugsCaught / 5);
        public double SpeedMultiplier { get; set; } = 1.0;
        public int PendingWebRequests { get; private set; }

        public bool HasVisualMotion
        {
            get
            {
                if (CurrentSpeed > 0.05 || ActiveSilkLine != null || DraglineAnchor != null)
                {
                    return true;
                }

                if (CurrentState is SpinningWebState or HuntingState or ShakingWebState or EatingFromPantryState or RappellingState or RepairingWebState or StoringPreyState or StartledByMouseState)
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

        private double _autoWebTimer;
        private int _scheduledForWebCount;

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
            // Eğer şu an zaten bir ağ örüyorsa (veya yarım kalan bir ağı varsa), önce onu bitirmesi için sıraya al
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
            _scheduledForWebCount = 0;
            _autoWebTimer = 0;
        }

        public void TransitionTo(ISpiderBehaviorState nextState, WorldContext world)
        {
            CurrentState.Exit(this, world);
            CurrentState = nextState;
            CurrentState.Enter(this, world);
        }

        public void RequestRappel(WorldContext world)
        {
            if (CurrentState is SpinningWebState) return;
            TransitionTo(new RappellingState(), world);
        }

        public void Update(WorldContext world, double dt)
        {
            VelocityVector = new Vector(0, 0);

            // 1. Fare İle Ürkütme Sensörü: Fare örümceğin üzerine hızla gelirse kaç!
            if (ShouldFleeFromCursor(world))
            {
                TransitionTo(new StartledByMouseState(CurrentState), world);
            }
            // 2. Av ve Açlık Sensörü:
            else if (CurrentState is not HuntingState and not StartledByMouseState and not StoringPreyState and not EatingFromPantryState)
            {
                BugEntity? targetBug = FindPrey(world);
                if (targetBug != null)
                {
                    TransitionTo(new HuntingState(targetBug, CurrentState), world);
                }
                // 3. Açlık Sensörü: Tokluk %50 ve altına indiğinde önce bulunduğu ağdaki kozaları,
                // o ağda koza yoksa diğer ağdaki kozaları yemeye gider!
                else if (Metabolism.IsHungry && CurrentState is not SpinningWebState)
                {
                    SpiderWebEntity? webWithFood = FindWebWithStoredFood(world);
                    if (webWithFood?.Pantry.PeekNextMeal() is { } storedCocoon)
                    {
                        TransitionTo(new EatingFromPantryState(webWithFood, storedCocoon), world);
                    }
                }
            }

            CurrentState.Update(this, world, dt);
            UpdateAutoWebSchedule(world, dt);
            Metabolism.Update(dt, CurrentSpeed);
            _gaitController.UpdateGait(this, dt);
        }

        private void UpdateAutoWebSchedule(WorldContext world, double dt)
        {
            int webCount = world.Webs.Count;
            if (webCount == 0)
            {
                _scheduledForWebCount = 0;
                _autoWebTimer = 0;
                return;
            }

            if (webCount >= MaxAutoWebs ||
                CurrentState is SpinningWebState ||
                PendingWebRequests > 0)
            {
                return;
            }

            for (int i = 0; i < webCount; i++)
            {
                if (!world.Webs[i].IsCompleted) return;
            }

            if (_scheduledForWebCount != webCount)
            {
                _scheduledForWebCount = webCount;
                _autoWebTimer = webCount switch
                {
                    1 => 2.5,      // Önce 2 ağ örülsün: 1. ağ biter bitmez (2.5 sn sonra) 2. ağa başlar
                    2 => 300.0,    // 2. ağdan 5 dakika (300 sn) sonra 3. ağ örülsün
                    3 => 600.0,    // 3. ağdan 10 dakika (600 sn) sonra 4. ağ örülsün
                    4 => 1800.0,   // 4. ağdan 30 dakika (1800 sn) sonra 5. ağ örülsün
                    _ => double.MaxValue
                };
            }

            _autoWebTimer -= dt;
            if (_autoWebTimer <= 0 &&
                CurrentState is WaitingOnWebState or WanderingState or InvestigatingVibrationState)
            {
                TransitionTo(SpinningWebState.CreateForBestSite(world), world);
            }
        }

        private bool ShouldFleeFromCursor(WorldContext world)
        {
            // Ağ örerken, avlanırken, iple sarkarken veya zaten kaçıyorken bölünmesin
            if (CurrentState is SpinningWebState or HuntingState or StartledByMouseState or RappellingState)
            {
                return false;
            }

            double distSq = (world.CursorPosition - Position).LengthSquared;
            return distSq < 62.0 * 62.0 && world.CursorSpeed > 420.0;
        }

        public bool MoveTowards(Point target, double speed, double dt, double stopDistance = 4.0)
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
            RotateTowards(desiredAngle, 12.5 * SpeedMultiplier, dt);

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

        public SpiderWebEntity? FindWebWithStoredFood(WorldContext world)
        {
            // 1. Öncelik: Örümcek şu an hangi ağda bulunuyorsa ve orada koza varsa önce oradakini seç!
            if (ActiveWeb != null && ActiveWeb.Pantry.HasStoredFood)
            {
                return ActiveWeb;
            }

            // 2. Bulunduğu ağda koza yoksa, kozası olan en yakın diğer ağa git! (Sıfır LINQ tahsisi)
            SpiderWebEntity? nearest = null;
            double bestDistSq = double.MaxValue;

            for (int i = 0; i < world.Webs.Count; i++)
            {
                var web = world.Webs[i];
                if (!web.Pantry.HasStoredFood) continue;

                double distSq = (web.Center - Position).LengthSquared;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    nearest = web;
                }
            }

            return nearest;
        }

        private BugEntity? FindPrey(WorldContext world)
        {
            // Örümcek ağın iplerini aktif olarak örüyorken işini yarım bırakmasın;
            // ancak ağ öreceği yere doğru henüz yürüyorsa (HasReachedSite == false) yolda denk geldiği böceği yakalayabilir.
            if (CurrentState is SpinningWebState { HasReachedSite: true })
            {
                return null;
            }

            const double encounterRadiusSq = 52.0 * 52.0;
            BugEntity? nearestEncounter = null;
            double bestEncounterDistSq = encounterRadiusSq;

            BugEntity? nearestTrapped = null;
            double bestTrappedDistSq = double.MaxValue;

            for (int i = 0; i < world.Bugs.Count; i++)
            {
                var bug = world.Bugs[i];
                double distSq = (bug.Position - Position).LengthSquared;

                if (bug.CurrentState.Status == BugLifecycleStatus.ActiveInWorld && distSq <= bestEncounterDistSq)
                {
                    bestEncounterDistSq = distSq;
                    nearestEncounter = bug;
                }
                else if (bug.IsReadyToBeHunted && distSq < bestTrappedDistSq)
                {
                    bestTrappedDistSq = distSq;
                    nearestTrapped = bug;
                }
            }

            return nearestEncounter ?? nearestTrapped;
        }

        private static double NormalizeAngle(double angle)
        {
            while (angle > Math.PI) angle -= Math.PI * 2;
            while (angle < -Math.PI) angle += Math.PI * 2;
            return angle;
        }
    }
}
