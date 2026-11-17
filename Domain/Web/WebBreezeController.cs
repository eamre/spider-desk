using System;
using System.Windows;

namespace SpiderDesk.Domain.Web
{
    /// <summary>
    /// Bitmiş bir ağa ara sıra esen hafif rüzgâr. Ağ bir süre durgun bekler, sonra birkaç saniyelik bir esintiyle
    /// yana doğru esner, yay gibi biraz geri gelir ve durulur. Bekleme süresi, esintinin süresi, gücü ve yönü her seferinde
    /// rastgele seçilir (WebTuning'in "Rüzgar Esintisi" bölümü).
    /// </summary>
    public sealed class WebBreezeController
    {
        // Bir esinti boyunca ağ 1.5 kez salınır: sağa esner, hafifçe geri gelir, durulur
        private const double SwingsPerGustTimesTwo = 3.0;

        // Esinti çoğunlukla yatay eser; dikey eğim -0.2 ile +0.3 arasındadır (hafifçe aşağı eğilimli)
        private const double VerticalTiltShift = 0.4;
        private const double VerticalTiltScale = 0.5;

        private double _cooldown = WebTuning.BreezeCooldownBase;
        private double _duration = WebTuning.BreezeDurationBase;
        private double _elapsed = double.PositiveInfinity; // Başta esinti yok; ilk esinti bekleme süresi dolunca başlar
        private double _maxStrength = WebTuning.BreezeStrengthBase;

        /// <summary>Ağın rüzgârla ne kadar esnediği (piksel). Esinti yokken 0.</summary>
        public double Offset { get; private set; }

        /// <summary>Rüzgârın estiği yön (birim vektör).</summary>
        public Vector Direction { get; private set; }

        /// <summary>Ağ şu an gözle görülür biçimde esniyorsa true döner.</summary>
        public bool IsBlowing => Math.Abs(Offset) > WebTuning.MotionThreshold;

        /// <summary>Rüzgârı bir kare ilerletir. Ağ henüz bitmediyse rüzgâr esmez ve bekleme süresi de işlemez.</summary>
        public void Update(double dt, bool isWebCompleted, Random rng)
        {
            if (!isWebCompleted)
            {
                Offset = 0;
            }
            else if (_elapsed < _duration)
            {
                Blow(dt, rng);
            }
            else
            {
                WaitForNextGust(dt, rng);
            }
        }

        private void Blow(double dt, Random rng)
        {
            _elapsed += dt;
            double progress = _elapsed / _duration;
            if (progress >= 1.0)
            {
                Offset = 0;
                _cooldown = WebTuning.BreezeCooldownBase + rng.NextDouble() * WebTuning.BreezeCooldownVariance;
                return;
            }

            // Yumuşak zarf eğrisi: esinti 0'dan başlar, ortada en güçlü olur, 0'da biter
            double envelope = Math.Sin(progress * Math.PI);
            double wave = Math.Sin(progress * Math.PI * SwingsPerGustTimesTwo);
            Offset = envelope * wave * _maxStrength;
        }

        private void WaitForNextGust(double dt, Random rng)
        {
            _cooldown -= dt;
            if (_cooldown > 0) return;

            _elapsed = 0;
            _duration = WebTuning.BreezeDurationBase + rng.NextDouble() * WebTuning.BreezeDurationVariance;
            _maxStrength = WebTuning.BreezeStrengthBase + rng.NextDouble() * WebTuning.BreezeStrengthVariance;

            double dirX = rng.NextDouble() > 0.5 ? 1.0 : -1.0;
            double dirY = (rng.NextDouble() - VerticalTiltShift) * VerticalTiltScale;
            Vector direction = new Vector(dirX, dirY);
            if (direction.Length > 0.001) direction.Normalize();
            Direction = direction;
        }
    }
}
