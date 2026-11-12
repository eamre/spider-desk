using System.Windows;

namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Örümceğin 8 bacağının adım atma koordinasyonunu (Tetrapod Gait) ve
    /// anatomik sınırlarını yönetir.
    /// </summary>
    public class SpiderGaitController
    {
        public SpiderLeg[] CreateLegs(SpiderEntity spider)
        {
            var legs = new SpiderLeg[8];

            // 4 çift bacak (Önden arkaya doğru doğal duruş açıları)
            double[] forwardOffsets = { 16, 6, -6, -17 };
            double[] sideReaches = { 24, 29, 28, 23 };
            double[] attachX = { 4.5, 1.5, -1.5, -4.5 };
            double[] femurLengths = { 17, 19, 19, 17 };
            double[] tibiaLengths = { 18, 20, 20, 18 };

            for (int i = 0; i < 8; i++)
            {
                int pairIdx = i / 2;
                int side = (i % 2 == 0) ? -1 : 1;
                int tetrapodGroup = ((pairIdx) + (i % 2)) % 2;

                Vector localAttach = new Vector(attachX[pairIdx], side * 5.0);
                Vector localRest = new Vector(forwardOffsets[pairIdx], side * sideReaches[pairIdx]);
                Point worldFoot = spider.LocalToWorld(localRest);

                legs[i] = new SpiderLeg(
                    index: i,
                    side: side,
                    tetrapodGroup: tetrapodGroup,
                    localAttach: localAttach,
                    localRestFoot: localRest,
                    femurLength: femurLengths[pairIdx],
                    tibiaLength: tibiaLengths[pairIdx],
                    initialWorldFoot: worldFoot
                );
            }

            return legs;
        }

        public void UpdateGait(SpiderEntity spider, double dt)
        {
            var legs = spider.Legs;
            // Adım hızı örümceğin yürüme hızına göre dinamik ölçeklenir (Hızlı koşarken hızlı adım atar)
            double stepRate = SpiderTuning.BaseStepRate + spider.CurrentSpeed * SpiderTuning.SpeedStepRateFactor;

            bool group0Busy = false;
            bool group1Busy = false;

            // 1. Devam eden adımları ilerlet
            foreach (var leg in legs)
            {
                if (leg.GestureFoot is { } gestureFoot)
                {
                    leg.CurrentFoot = gestureFoot;
                    leg.StepProgress = 1.0;
                    continue;
                }

                if (leg.IsStepping)
                {
                    leg.StepProgress += dt * stepRate;
                    if (leg.StepProgress >= 1.0)
                    {
                        leg.StepProgress = 1.0;
                        leg.CurrentFoot = leg.StepTarget;
                    }
                    else
                    {
                        double t = leg.StepProgress;
                        double smooth = t * t * (3.0 - 2.0 * t);
                        // Adım hedefi örümcek hareket ettikçe güncel ideal konuma doğru kayar
                        Point dynamicTarget = spider.LocalToWorld(leg.LocalRestFoot) + spider.VelocityVector * SpiderTuning.DynamicStepLeadTime;
                        leg.StepTarget = dynamicTarget;

                        leg.CurrentFoot = new Point(
                            leg.StepStart.X + (leg.StepTarget.X - leg.StepStart.X) * smooth,
                            leg.StepStart.Y + (leg.StepTarget.Y - leg.StepStart.Y) * smooth
                        );
                    }

                    if (leg.IsStepping)
                    {
                        if (leg.TetrapodGroup == 0) group0Busy = true;
                        else group1Busy = true;
                    }
                }
            }

            // 2. İdeal noktasından uzaklaşan bacaklar için yeni adım başlat
            foreach (var leg in legs)
            {
                if (leg.GestureFoot != null) continue;

                Point hip = spider.LocalToWorld(leg.LocalAttach);
                Point idealFoot = spider.LocalToWorld(leg.LocalRestFoot);

                if (!leg.IsStepping)
                {
                    double distFromIdeal = (idealFoot - leg.CurrentFoot).Length;
                    bool isEmergency = distFromIdeal >= SpiderTuning.EmergencyStepDistance;
                    bool canGroupStep = (leg.TetrapodGroup == 0 && !group1Busy) || (leg.TetrapodGroup == 1 && !group0Busy);

                    if (isEmergency || (distFromIdeal >= SpiderTuning.NormalStepTriggerDistance && canGroupStep))
                    {
                        leg.StepStart = leg.CurrentFoot;
                        leg.StepTarget = idealFoot + spider.VelocityVector * SpiderTuning.InitialStepLeadTime;
                        leg.StepProgress = 0.0;

                        if (leg.TetrapodGroup == 0) group0Busy = true;
                        else group1Busy = true;
                    }
                }

                // 3. Kesin Anatomik Kısıtlama (Clamping): Bacak hiçbir koşulda kopup uzayamaz
                Vector hipToFoot = leg.CurrentFoot - hip;
                double currentReach = hipToFoot.Length;
                if (currentReach > leg.MaxReach && currentReach > 0.001)
                {
                    leg.CurrentFoot = hip + (hipToFoot / currentReach) * leg.MaxReach;
                }
            }
        }
    }
}
