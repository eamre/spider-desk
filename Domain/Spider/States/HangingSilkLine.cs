using System;
using System.Windows;

namespace SpiderDesk.Domain.Spider.States
{
    /// <summary>
    /// Örümceğin tavandaki bir noktadan ipek iple sarkarken izlediği hareket: ipin boyu, sallanma ve ipin kendisi.
    /// İple sarkan durumlar (RappellingState, DeliveringNoteState) aynı hareketi bu sınıf üzerinden paylaşır.
    /// </summary>
    public sealed class HangingSilkLine
    {
        private double _swayTime;

        public Point CeilingAnchor { get; }

        /// <summary>Tavandan örümceğe kadar ipin boyu (piksel).</summary>
        public double Length { get; set; }

        /// <summary>Sallanmanın genişliği; ip uzadıkça etkisi artar.</summary>
        public double SwayAmplitude { get; set; }

        public HangingSilkLine(Point ceilingAnchor, double length, double swayAmplitude)
        {
            CeilingAnchor = ceilingAnchor;
            Length = length;
            SwayAmplitude = swayAmplitude;
        }

        /// <summary>Örümceği ipin ucuna taşır, sallandırır ve ipi tavandan örümceğin karnına çizer.</summary>
        /// <param name="hangAlongThread">
        /// True ise örümcek sallanırken her küçük yana kaymaya dönmez, gerçek bir örümcek gibi ipiyle aynı doğrultuda,
        /// baş aşağı sarkar. Böylece tuttuğu not birkaç dereceden fazla eğilmez.
        /// </param>
        public void MoveSpider(SpiderEntity spider, double dt, bool facingUp = false, bool hangAlongThread = false)
        {
            _swayTime += dt * 2.8;
            double offsetX = Math.Sin(_swayTime) * SwayAmplitude * (Length / 220.0);

            Point targetPos = new Point(CeilingAnchor.X + offsetX, Length);
            double angleBeforeMove = spider.Angle;
            spider.MoveTowards(targetPos, 350.0, dt, stopDistance: 0.5);

            double desiredAngle = facingUp ? -Math.PI / 2.0 : Math.PI / 2.0;
            if (hangAlongThread)
            {
                // MoveTowards'ın gidiş yönüne çevirmesini geri al; örümcek yalnızca ipin doğrultusuna döner
                spider.RotateBy(angleBeforeMove - spider.Angle);
                Vector thread = spider.Position - CeilingAnchor;
                desiredAngle = Math.Atan2(thread.Y, thread.X);
            }
            spider.RotateTowards(desiredAngle, 10.0, dt);

            spider.ActiveSilkLine = (CeilingAnchor, spider.GetSpinneretPosition());
        }

        /// <summary>Fare sarkan örümceğe hızla yaklaşıyorsa true döner; örümcek o zaman yukarı kaçar.</summary>
        public static bool IsCursorPushing(SpiderEntity spider, WorldContext world)
        {
            const double pushRadiusSq = SpiderTuning.RappelCursorPushDistance * SpiderTuning.RappelCursorPushDistance;
            double distSq = (world.CursorPosition - spider.Position).LengthSquared;
            return distSq < pushRadiusSq && world.CursorSpeed > SpiderTuning.RappelCursorPushSpeed;
        }
    }
}
