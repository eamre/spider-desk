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

        // Ağ Nöbeti ve Devriye (WaitingOnWebState)
        public const double WebPatrolInitialDurationBase = 10.0;
        public const double WebPatrolInitialDurationVariance = 8.0;
        public const double WebPatrolSubsequentDurationBase = 6.0;
        public const double WebPatrolSubsequentDurationVariance = 6.0;
        public const double WebRotationChance = 0.45;
        public const double WebPatrolRadiusMinRatio = 0.18;
        public const double WebPatrolRadiusSpanRatio = 0.28;
        public const double HubApproachStopDistance = 3.5;

        // Kiler ve Erzak Depolama (WebPantryController & WebRenderer)
        public const double CocoonClearanceDistance = 28.0;
        public const double CocoonClearanceDistanceSquared = CocoonClearanceDistance * CocoonClearanceDistance;
        public const double PantryInnerRadiusRatio = 0.36;
        public const double PantryOuterRadiusSpan = 0.46;
        public const double CocoonConsumedScaleReduction = 0.85;

        // Çiy Damlası Fiziği (WebDropletController)
        public const int MaxAttachedDropletsPerWeb = 12;
        public const double MaxDropletSag = 14.0;
        public const double DropletSagMultiplier = 2.4;
        public const double InitialDropletRadiusBase = 2.8;
        public const double InitialDropletRadiusVariance = 0.9;

        // Rüzgar Esintisi Fiziği (SpiderWebEntity)
        public const double BreezeCooldownBase = 22.0;
        public const double BreezeCooldownVariance = 28.0;
        public const double BreezeDurationBase = 2.2;
        public const double BreezeDurationVariance = 0.8;
        public const double BreezeStrengthBase = 3.5;
        public const double BreezeStrengthVariance = 2.0;

        // Ağ Hareket Algılama (titreşim/rüzgar bu eşiğin altındaysa ağ durgun sayılır ve önbellekten çizilir)
        public const double WebMotionThreshold = 0.05;

        // Açılış Selamı (WritingGreetingState & SilkWriting)
        public const double GreetingTextCenterYRatio = 0.3;
        public const double GreetingTextWidthRatio = 0.8;
        public const double GreetingMaxLetterHeight = 110.0;
        public const double GreetingStandOffsetLetters = 1.2;
        public const double GreetingStandBottomMargin = 110.0;
        public const double GreetingApproachSpeed = 240.0;
        public const double GreetingAimTurnSpeed = 7.0;
        public const double GreetingTrackTurnSpeed = 10.0;
        public const double GreetingAimTolerance = 0.05;
        public const double GreetingAimTimeoutSeconds = 0.9;
        public const double GreetingLaunchBaseSeconds = 0.16;
        public const double GreetingLaunchSecondsPerPixel = 1.0 / 1500.0;
        public const double GreetingLaunchArcRatio = 0.22;
        public const double GreetingTargetWriteSeconds = 5.0;
        public const double GreetingMinStrokeSpeed = 330.0;
        public const double GreetingHopSpeedFactor = 2.0;
        public const double GreetingAdmireSeconds = 2.6;
        public const double GreetingAdmireTurnSpeed = 4.0;
        public const double GreetingWaveStartSeconds = 0.6;
        public const double GreetingWaveEndSeconds = 2.2;
        public const double GreetingWaveFrequency = 11.0;
        public const double GreetingFadeSeconds = 1.8;
        public const double GreetingBeadSpacing = 24.0;
        public const double GreetingBeadMinRadius = 0.9;
        public const double GreetingBeadRadiusVariance = 0.9;
    }
}
