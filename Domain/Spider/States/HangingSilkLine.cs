using System;
using System.Windows;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin tavandaki bir noktadan ipek iple sarkarken izlediği hareket: ipin boyu, sallanma ve ipin kendisi.
    /// İple sarkan durumlar (RappellingState, DeliveringNoteState) aynı hareketi bu sınıf üzerinden paylaşır.
    /// Bütün ayar sayıları SpiderTuning'in "İple Sarkma" bölümündedir.
    /// </summary>
    public sealed class HangingSilkLine
    {
        /// <summary>Örümceğin açısı baş aşağı sarkarken (ekranda aşağıyı gösterir).</summary>
        public const double HeadDownAngle = Math.PI / 2.0;

        /// <summary>Örümceğin açısı ipe tırmanırken (ekranda yukarıyı gösterir).</summary>
        public const double HeadUpAngle = -Math.PI / 2.0;

        private double _swayTime;

        public Point CeilingAnchor { get; }

        /// <summary>Tavandan örümceğe kadar ipin boyu (piksel).</summary>
        public double Length { get; set; }

        /// <summary>Sallanmanın genişliği; ip uzadıkça etkisi artar.</summary>
        public double SwayAmplitude { get; set; }

        /// <summary>İp yeterince kısaldı mı? O zaman örümcek tavana varmış sayılır.</summary>
        public bool IsReeledIn => Length <= SpiderTuning.RappelLineLengthAtTop;

        /// <summary>
        /// Örümcek tavana varınca ipi ekranın üst kenarına bağlar. İp, örümceğin tavanda durduğu yüksekliğe kadar uzanır
        /// ve hafif bir sallanmayla başlar.
        /// </summary>
        public HangingSilkLine(double anchorX)
        {
            CeilingAnchor = new Point(anchorX, SpiderTuning.RappelThreadAnchorY);
            Length = SpiderTuning.RappelCeilingY;
            SwayAmplitude = SpiderTuning.RappelStartSwayAmplitude;
        }

        /// <summary>Örümceğin ipi bağlamak için tırmandığı nokta (sarkma noktasının tam altında, tavanda).</summary>
        public static Point ClimbTarget(double anchorX) => new(anchorX, SpiderTuning.RappelCeilingY);

        /// <summary>Örümceği ipin ucuna taşır, sallandırır ve ipi tavandan örümceğin karnına çizer.</summary>
        /// <param name="hangAlongThread">
        /// True ise örümcek sallanırken her küçük yana kaymaya dönmez, gerçek bir örümcek gibi ipiyle aynı doğrultuda,
        /// baş aşağı sarkar. Böylece tuttuğu not birkaç dereceden fazla eğilmez.
        /// </param>
        public void MoveSpider(SpiderEntity spider, double dt, bool facingUp = false, bool hangAlongThread = false)
        {
            _swayTime += dt * SpiderTuning.RappelSwayFrequency;
            double offsetX = Math.Sin(_swayTime) * SwayAmplitude * (Length / SpiderTuning.RappelSwayFullLength);

            Point targetPos = new Point(CeilingAnchor.X + offsetX, Length);
            double angleBeforeMove = spider.Angle;
            spider.MoveTowards(targetPos, SpiderTuning.RappelFollowSpeed, dt, stopDistance: SpiderTuning.RappelFollowStopDistance);

            double desiredAngle = facingUp ? HeadUpAngle : HeadDownAngle;
            if (hangAlongThread)
            {
                // MoveTowards'ın gidiş yönüne çevirmesini geri al; örümcek yalnızca ipin doğrultusuna döner
                spider.RotateBy(angleBeforeMove - spider.Angle);
                Vector thread = spider.Position - CeilingAnchor;
                desiredAngle = Math.Atan2(thread.Y, thread.X);
            }
            spider.RotateTowards(desiredAngle, SpiderTuning.RappelTurnSpeed, dt);

            spider.ActiveSilkLine = (CeilingAnchor, spider.GetSpinneretPosition());
        }

        /// <summary>
        /// Sallanmayı yavaşça söndürür. Tamamen durmaz; en az <see cref="SpiderTuning.RappelMinSwayAmplitude"/> kalır
        /// ki asılı örümcek hafifçe sallanmaya devam etsin.
        /// </summary>
        public void DampSway(double dt) =>
            SwayAmplitude = Math.Max(
                SpiderTuning.RappelMinSwayAmplitude,
                SwayAmplitude * Math.Pow(SpiderTuning.RappelSwayDampingPerSecond, dt));

        /// <summary>Fare sarkan örümceğe hızla yaklaşıyorsa true döner; örümcek o zaman yukarı kaçar.</summary>
        public static bool IsCursorPushing(SpiderEntity spider, WorldContext world)
        {
            const double pushRadiusSq = SpiderTuning.RappelCursorPushDistance * SpiderTuning.RappelCursorPushDistance;
            double distSq = (world.CursorPosition - spider.Position).LengthSquared;
            return distSq < pushRadiusSq && world.CursorSpeed > SpiderTuning.RappelCursorPushSpeed;
        }
    }
}
