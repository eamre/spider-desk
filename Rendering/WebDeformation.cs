using System;
using System.Windows;
using SpiderDesk.Domain.Web;

namespace SpiderDesk.Rendering
{
    /// <summary>
    /// Bir ağın tek bir karedeki rüzgar ve titreşim kaynaklı şekil bozulmasının anlık görüntüsü.
    /// Tellerin, damlaların, kopuk tellerin ve kozaların aynı formülle kaydırılmasını sağlar (DRY).
    /// Ağ durgunsa <see cref="IsStill"/> true olur ve hiçbir nokta kaydırılmaz.
    /// </summary>
    internal readonly struct WebDeformation
    {
        private const double BreezeReachFactor = 1.05;
        private const double BreezeCurveCenterFactor = 0.75;
        private const double VibrationReachFactor = 1.5;
        private const double VibrationWavelengthFactor = 0.08;
        private const double VibrationDisplacementFactor = 0.5;

        private readonly Point _center;
        private readonly bool _isBreezing;
        private readonly Vector _breezeDirection;
        private readonly double _breezeOffset;
        private readonly double _breezeReach;

        private readonly bool _isVibrating;
        private readonly Point _vibrationOrigin;
        private readonly double _vibrationPhase;
        private readonly double _vibrationAmplitude;
        private readonly double _vibrationReach;

        private WebDeformation(SpiderWebEntity web, bool includeMotion)
        {
            _center = web.Center;

            _isBreezing = includeMotion && web.IsBreezing;
            _breezeDirection = web.BreezeDirection;
            _breezeOffset = web.BreezeOffset;
            _breezeReach = web.Radius * BreezeReachFactor;

            _isVibrating = includeMotion && web.IsVibrating;
            _vibrationOrigin = web.VibrationOrigin;
            _vibrationPhase = web.VibrationPhase;
            _vibrationAmplitude = web.VibrationAmplitude;
            _vibrationReach = web.Radius * VibrationReachFactor;
        }

        /// <summary>Ağın şu anki rüzgar ve titreşim durumunu yansıtan bozulma.</summary>
        public static WebDeformation Current(SpiderWebEntity web) => new(web, includeMotion: true);

        /// <summary>Hiçbir noktayı kaydırmayan, durgun ağ bozulması (önbellekli çizim için).</summary>
        public static WebDeformation Still(SpiderWebEntity web) => new(web, includeMotion: false);

        /// <summary>Hiçbir ağa bağlı olmayan varlıklar için: hiçbir noktayı kaydırmaz.</summary>
        public static WebDeformation None => default;

        /// <summary>
        /// Varlık bir ağın üzerindeyse o ağın anlık bozulmasını, değilse <see cref="None"/> döner.
        /// Rüzgar ağın kenarında sıfıra indiği için ağın dışında kalan noktalar zaten kaymaz.
        /// </summary>
        public static WebDeformation CurrentOrNone(SpiderWebEntity? web) => web is null ? None : Current(web);

        public bool IsStill => !_isBreezing && !_isVibrating;

        /// <summary>
        /// Spiral tellerin kavis hesabında kullanılan merkez. Rüzgarda ağın göbeği de hafifçe sürüklenir.
        /// </summary>
        public Point CurveCenter => _isBreezing
            ? _center + _breezeDirection * (_breezeOffset * BreezeCurveCenterFactor)
            : _center;

        /// <summary>
        /// Rüzgarın verilen noktayı ne kadar ittiği (işaretli, piksel). Göbekte en güçlü, kenarda sıfırdır.
        /// </summary>
        public double BreezeInfluence(Point p)
        {
            if (!_isBreezing) return 0.0;

            double falloff = Math.Max(0.0, 1.0 - ((p - _center).Length / _breezeReach));
            return _breezeOffset * falloff * falloff;
        }

        /// <summary>
        /// Noktayı rüzgar ve titreşimle kaydırır. Sonuç yalnızca noktanın konumuna bağlıdır;
        /// böylece aynı noktayı paylaşan teller hiçbir zaman birbirinden ayrılmaz.
        /// </summary>
        public Point Displace(Point p)
        {
            Point displaced = p;

            if (_isBreezing)
            {
                displaced += _breezeDirection * BreezeInfluence(p);
            }

            if (_isVibrating)
            {
                double wave = VibrationWave(p);
                displaced.X += wave;
                displaced.Y += wave;
            }

            return displaced;
        }

        private double VibrationWave(Point p)
        {
            double distance = (p - _vibrationOrigin).Length;
            double falloff = Math.Max(0.0, 1.0 - distance / _vibrationReach);
            return Math.Sin(_vibrationPhase - distance * VibrationWavelengthFactor) * _vibrationAmplitude * falloff * VibrationDisplacementFactor;
        }
    }
}
