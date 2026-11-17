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

        // Not Getirme (DeliveringNoteState): örümcek baş aşağı sarkar, notu ön iki bacağıyla tutar
        public const double NoteHoldDistance = 30.0;      // Gövde merkezinden kartın üst kenarına (başın önünde)
        public const double NoteGripHalfSpan = 15.0;      // Ön bacakların kartı tuttuğu noktalar, ortadan sağa/sola
        public const double NoteHangSeconds = 20.0;
        public const double NoteHangHeightRatio = 0.30;   // Kartın okunacağı yükseklik, ekran yüksekliğine oranla
        public const double NoteScreenEdgeMargin = 170.0; // Geniş kart ekrandan taşmasın
        public const double NoteFadeInSeconds = 0.4;
        public const double NoteUprightTolerance = 0.2;   // Örümcek baş aşağı konuma bu kadar (radyan) yaklaşınca not belirir
        public const double NoteFadeOutDistance = 90.0;   // Tavana bu kadar kala kart tamamen solar

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

        // İpek Yazısı (WritingSilkMessageState & SilkWriting): açılış selamı ve Pomodoro duyuruları
        public const double SilkWritingTextCenterYRatio = 0.3;
        public const double SilkWritingTextWidthRatio = 0.8;
        public const double SilkWritingMaxLetterHeight = 110.0;
        public const double SilkWritingStandOffsetLetters = 1.2;
        public const double SilkWritingStandBottomMargin = 110.0;
        public const double SilkWritingApproachSpeed = 240.0;
        public const double SilkWritingAimTurnSpeed = 7.0;
        public const double SilkWritingTrackTurnSpeed = 10.0;
        public const double SilkWritingAimTolerance = 0.05;
        public const double SilkWritingAimTimeoutSeconds = 0.9;
        public const double SilkWritingLaunchBaseSeconds = 0.16;
        public const double SilkWritingLaunchSecondsPerPixel = 1.0 / 1500.0;
        public const double SilkWritingLaunchArcRatio = 0.22;
        public const double SilkWritingTargetWriteSeconds = 5.0;
        public const double SilkWritingMinStrokeSpeed = 330.0;
        public const double SilkWritingHopSpeedFactor = 2.0;
        public const double SilkWritingAdmireSeconds = 2.6;
        public const double SilkWritingAdmireTurnSpeed = 4.0;
        public const double SilkWritingWaveStartSeconds = 0.6;
        public const double SilkWritingWaveEndSeconds = 2.2;
        public const double SilkWritingWaveFrequency = 11.0;
        public const double SilkWritingFadeSeconds = 1.8;
        public const double SilkWritingBeadSpacing = 24.0;
        public const double SilkWritingBeadMinRadius = 0.9;
        public const double SilkWritingBeadRadiusVariance = 0.9;
    }
}
