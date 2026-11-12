using System;
using System.Windows;

namespace SpiderDesk.Domain.Spider
{
    public readonly record struct LegJointPositions(Point Hip, Point Knee, Point Foot);

    /// <summary>
    /// Tek bir örümcek bacağının anatomik ölçülerini ve 2-Kemikli Ters Kinematik (2-Bone IK)
    /// hesaplamasını barındırır. Bacak uzunluğu sabit olduğu için asla uzamaz.
    /// </summary>
    public class SpiderLeg
    {
        public int Index { get; }
        public int PairIndex => Index / 2; // 0, 1 (Ön çiftler), 2, 3 (Arka çiftler)
        public int Side { get; } // -1: Sol, +1: Sağ
        public int TetrapodGroup { get; } // 0 veya 1 (Alternatif adım grubu)

        public Vector LocalAttach { get; }
        public Vector LocalRestFoot { get; }
        public double FemurLength { get; }
        public double TibiaLength { get; }
        public double MaxReach => (FemurLength + TibiaLength) * 0.96;

        public Point CurrentFoot { get; set; }
        public Point StepStart { get; set; }
        public Point StepTarget { get; set; }
        public double StepProgress { get; set; } = 1.0;
        public bool IsStepping => StepProgress < 1.0;

        /// <summary>
        /// Bir durum bacağı yürüyüşten bağımsız bir harekette (örn. el sallama) tutuyorsa ayağın gideceği nokta.
        /// Doluyken yürüyüş kontrolcüsü bu bacakla adım atmaz; boşaltılınca bacak bir adımla dinlenme yerine döner.
        /// </summary>
        public Point? GestureFoot { get; set; }

        public SpiderLeg(
            int index,
            int side,
            int tetrapodGroup,
            Vector localAttach,
            Vector localRestFoot,
            double femurLength,
            double tibiaLength,
            Point initialWorldFoot)
        {
            Index = index;
            Side = side;
            TetrapodGroup = tetrapodGroup;
            LocalAttach = localAttach;
            LocalRestFoot = localRestFoot;
            FemurLength = femurLength;
            TibiaLength = tibiaLength;

            CurrentFoot = initialWorldFoot;
            StepStart = initialWorldFoot;
            StepTarget = initialWorldFoot;
        }

        /// <summary>
        /// Kosinüs Teoremi ile gerçek 2-Segmentli Ters Kinematik (2-Bone IK) çözer.
        /// Üstten görünümde ön bacakların dizleri hafif öne, arka bacakların dizleri hafif arkaya/dışa bükülür.
        /// </summary>
        public LegJointPositions SolveJoints(Point hip)
        {
            Point targetFoot = CurrentFoot;
            Vector hipToFoot = targetFoot - hip;
            double dist = hipToFoot.Length;

            if (dist > MaxReach && dist > 0.001)
            {
                hipToFoot = (hipToFoot / dist) * MaxReach;
                targetFoot = hip + hipToFoot;
                dist = MaxReach;
            }
            else if (dist < 4.0)
            {
                dist = 4.0;
            }

            double a = FemurLength;
            double b = TibiaLength;
            double c = Math.Clamp(dist, Math.Abs(a - b) + 0.5, a + b - 0.5);

            double cosAngle = (a * a + c * c - b * b) / (2.0 * a * c);
            cosAngle = Math.Clamp(cosAngle, -1.0, 1.0);
            double kneeBendAngle = Math.Acos(cosAngle);

            Vector dir = hipToFoot / dist;

            // Ön 2 çift bacak (PairIndex 0,1) ile arka 2 çift bacak (PairIndex 2,3) simetrik kavis alır
            double bendSign = (PairIndex < 2) ? -Side : Side;
            Vector perp = new Vector(-dir.Y * bendSign, dir.X * bendSign);

            double femurProj = a * Math.Cos(kneeBendAngle);
            double kneeOffset = a * Math.Sin(kneeBendAngle) * 0.55;

            Point knee = new Point(
                hip.X + dir.X * femurProj + perp.X * kneeOffset,
                hip.Y + dir.Y * femurProj + perp.Y * kneeOffset
            );

            return new LegJointPositions(hip, knee, targetFoot);
        }
    }
}
