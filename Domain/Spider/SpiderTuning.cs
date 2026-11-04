namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// Örümceğin hareket, avlanma, ağ örme, metabolizma ve etkileşim sabitlerini (sihirli sayıları)
    /// tek bir merkezde toplayan dengeleme (tuning/balance) sınıfı.
    /// </summary>
    public static class SpiderTuning
    {
        // Seviye ve Gelişim
        public const int BugsPerLevel = 5;

        // Hareket ve Dönüş
        public const double DefaultTurnSpeed = 12.5;
        public const double DefaultStopDistance = 4.0;
        public const double WanderMoveSpeed = 130.0;
        public const double PatrolMoveSpeed = 110.0;
        public const double ApproachHubSpeed = 155.0;
        public const double StorePreyMoveSpeed = 155.0;
        public const double EatMoveSpeed = 170.0;
        public const double ApproachWebSiteSpeed = 175.0;
        public const double SpinAnchorSpeed = 195.0;
        public const double SpinSpiralSpeed = 230.0;
        public const double RepairWalkSpeed = 185.0;
        public const double RepairSpinSpeed = 200.0;
        public const double InvestigateMoveSpeed = 185.0;

        // Fare Etkileşimi ve Ürkme (Startle)
        public const double CursorStartleDistance = 62.0;
        public const double CursorStartleMinSpeed = 420.0;
        public const double StartleFleeMinDistance = 150.0;
        public const double StartleFleeDistanceRange = 70.0;
        public const double StartleFleeSpeed = 285.0;
        public const double StartleAlertPauseSeconds = 0.8;

        // Avlanma ve Beslenme
        public const double DesktopEncounterRadius = 52.0;
        public const double WildBugMaxChaseDistance = 135.0;
        public const double ChaseTrappedBugSpeed = 260.0;
        public const double ChaseWildBugSpeed = 310.0;
        public const double TrappedCatchStopDistance = 14.0;
        public const double WildCatchStopDistance = 18.0;
        public const double WrapProgressRate = 0.85;
        public const double CocoonConsumeRate = 0.65;
        public const double PreyNutrition = 25.0;

        // Metabolizma
        public const double InitialSatiety = 70.0;
        public const double FullSatietyThreshold = 75.0;
        public const double HungrySatietyThreshold = 50.0;
        public const double BasalBurnRatePerSecond = 0.65;
        public const double MovementBurnFactor = 0.0045;

        // Yürüyüş Fiziği (Tetrapod Gait)
        public const double NormalStepTriggerDistance = 11.5;
        public const double EmergencyStepDistance = 19.0;
        public const double BaseStepRate = 12.0;
        public const double SpeedStepRateFactor = 0.065;
        public const double DynamicStepLeadTime = 0.045;
        public const double InitialStepLeadTime = 0.05;

        // İple Sarkma (Rappelling)
        public const double RappelClimbSpeed = 165.0;
        public const double RappelDescendSpeed = 115.0;
        public const double RappelAscendSpeed = 145.0;
        public const double RappelCursorPushDistance = 72.0;
        public const double RappelCursorPushSpeed = 80.0;

        // Ağ Yakalama ve Hasar Etkileşimi
        public const double WebCatchDistanceSquared = 196.0;
        public const double WebCursorHitDistanceSquared = 100.0;
        public const double WebCursorTearSpeed = 1350.0;
        public const double WebCursorDamageRadius = 28.0;
        public const double WebPluckCooldownSeconds = 0.25;
    }
}
