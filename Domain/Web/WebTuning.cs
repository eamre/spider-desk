namespace SpiderDesk.Domain.Web
{
    /// <summary>
    /// Ağın fiziğini belirleyen sabitler: titreşim, rüzgar, çiy damlaları, böcek yakalama ve fareyle etkileşim.
    /// Örümceğin hareket ve davranış sabitleri SpiderTuning'dedir.
    /// </summary>
    public static class WebTuning
    {
        // Ağ Sayısı ve Yerleşimi (WorldContext & WebSitePlanner)
        public const int MaxWebsOnScreen = 6;                // Yeni ağ bu sayıyı aşarsa en eski ağ silinir
        public const double CornerFanHubDistance = 58.0;      // Köşe ağında örümceğin beklediği nokta, köşeden bu kadar içeride

        // Hareket Algılama (titreşim/rüzgar bu eşiğin altındaysa ağ durgun sayılır ve önbellekten çizilir)
        public const double MotionThreshold = 0.05;

        // Titreşim
        public const double DefaultVibrationStrength = 6.0;
        public const double MaxVibrationAmplitude = 10.0;
        public const double VibrationFrequency = 38.0;
        public const double VibrationDecayPerSecond = 0.18; // Her saniye genliğin kalan oranı
        public const double TearVibrationStrength = 9.0;

        // Rüzgar Esintisi
        public const double BreezeCooldownBase = 22.0;
        public const double BreezeCooldownVariance = 28.0;
        public const double BreezeDurationBase = 2.2;
        public const double BreezeDurationVariance = 0.8;
        public const double BreezeStrengthBase = 3.5;
        public const double BreezeStrengthVariance = 2.0;

        // Böcek Yakalama ve Fareyle Etkileşim
        public const double InteractionRadiusSquaredFactor = 1.35; // Ağ yarıçapının karesine uygulanır
        public const double CatchDistanceSquared = 196.0;
        public const double CursorHitDistanceSquared = 100.0;
        public const double PluckMinCursorSpeed = 25.0;
        public const double PluckStrengthPerCursorSpeed = 0.012;
        public const double MinPluckStrength = 2.5;
        public const double MaxPluckStrength = 7.5;
        public const double PluckCooldownSeconds = 0.25;
        public const double CursorTearSpeed = 1350.0;
        public const double CursorDamageRadius = 28.0;

        // Çiy Damlaları
        public const double InitialCondensationDelay = 14.0;
        public const double DewGrowthPerSecond = 0.08;
        public const int MaxDewDropletsPerWeb = 9;            // Kendiliğinden oluşan çiy için sınır
        public const int MaxAttachedDropletsPerWeb = 12;      // Yağmur dahil toplam sınır
        public const int HeavyWithWaterDropletCount = 6;      // Bu kadar damla birikince örümcek ağı silkeler
        public const double MaxDropletSag = 14.0;
        public const double DropletSagMultiplier = 2.4;
        public const double InitialDropletRadiusBase = 2.8;
        public const double InitialDropletRadiusVariance = 0.9;
        public const double ShakenDropletFallSpeedBase = 45.0;
        public const double ShakenDropletFallSpeedVariance = 65.0;
        public const double BrokenThreadDropletFallSpeedBase = 35.0;
        public const double BrokenThreadDropletFallSpeedVariance = 55.0;
    }
}
