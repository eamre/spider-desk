using System;

namespace SpiderDesk.Infrastructure.Flyout
{
    /// <summary>
    /// Tepsi ikonuna tıklamanın pencereyi açıp açmayacağına karar veren saf kural.
    /// Pencere açıkken ikona basmak önce pencerenin dışına tıklamak sayılır ve pencere kendini gizler; hemen ardından
    /// gelen ikon tıklaması onu yeniden açmamalıdır. Yoksa ikona basmak pencereyi kapatıp açardı ve pencere titrerdi.
    /// </summary>
    public static class FlyoutToggle
    {
        /// <summary>Pencere bundan kısa süre önce kendiliğinden gizlendiyse, o gizlenmeye yol açan tıklama bu sayılır.</summary>
        public static readonly TimeSpan SameClickWindow = TimeSpan.FromMilliseconds(400);

        /// <param name="isVisible">Pencere şu an açık mı?</param>
        /// <param name="lastAutoHiddenAt">Pencere en son dışarı tıklanınca ne zaman gizlendi? Hiç gizlenmediyse null.</param>
        /// <param name="now">Şu an.</param>
        public static bool ShouldOpen(bool isVisible, DateTime? lastAutoHiddenAt, DateTime now)
        {
            if (isVisible) return false;
            return lastAutoHiddenAt is not { } hiddenAt || now - hiddenAt >= SameClickWindow;
        }
    }
}
