namespace SpiderDesk.Domain.Spider
{
    /// <summary>
    /// "Örümcek" sekmesinde gösterilen durum: seviye, bir sonraki seviyeye kalan av, tokluk, toplam av ve şu an ne yaptığı.
    /// Örümcek her <see cref="SpiderTuning.BugsPerLevel"/> avda bir seviye atlar.
    /// </summary>
    /// <param name="LevelProgress">Bu seviyede yakalanan av oranı: 0 seviyenin başı, 1'e yaklaştıkça bir sonraki seviye yakın.</param>
    public readonly record struct SpiderStatusDisplay(
        int Level,
        string NextLevelText,
        double LevelProgress,
        int SatietyPercent,
        int BugsCaught,
        string Activity)
    {
        public static SpiderStatusDisplay For(int bugsCaught, int satietyPercent, string activity)
        {
            int level = 1 + bugsCaught / SpiderTuning.BugsPerLevel;
            int caughtThisLevel = bugsCaught % SpiderTuning.BugsPerLevel;
            int remaining = SpiderTuning.BugsPerLevel - caughtThisLevel;

            return new SpiderStatusDisplay(
                level,
                $"{remaining} av sonra seviye {level + 1}",
                (double)caughtThisLevel / SpiderTuning.BugsPerLevel,
                satietyPercent,
                bugsCaught,
                activity);
        }
    }
}
