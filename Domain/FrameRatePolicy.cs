using System;

namespace SpiderDesk.Domain
{
    /// <summary>
    /// Kare hızı yöneticisi için karar çıktısı.
    /// Hedef zaman aralığı, motorun güncellenip güncellenmeyeceği ve render yapılıp yapılmayacağını belirtir.
    /// </summary>
    public readonly record struct FrameRateDecision(
        TimeSpan TargetInterval,
        bool ShouldUpdateEngine,
        bool ShouldRenderCanvas);

    /// <summary>
    /// Simülasyonun duraklatılma, örtülme (occlusion) ve görsel hareketlilik durumlarına göre
    /// hedef kare hızını (FPS) ve render kararlarını belirleyen saf kural sınıfı (Pure Policy).
    /// Hiçbir dış bağımlılığı (WPF, Win32 vb.) olmadığı için %100 test edilebilirdir.
    /// </summary>
    public static class FrameRatePolicy
    {
        public static readonly TimeSpan ActiveFrameInterval = TimeSpan.FromMilliseconds(16); // ~60 FPS (Hareket varken)
        public static readonly TimeSpan IdleFrameInterval = TimeSpan.FromMilliseconds(50);   // 20 Hz (Durağanken veya arka plandayken)

        public static FrameRateDecision Decide(bool isPaused, bool isOccluded, bool hasVisualActivity)
        {
            if (isPaused)
            {
                // Duraklatıldığında: Simülasyon ve render durur, düşük frekansta (20 Hz) olay beklenir
                return new FrameRateDecision(IdleFrameInterval, ShouldUpdateEngine: false, ShouldRenderCanvas: false);
            }

            if (isOccluded)
            {
                // Masaüstü başka bir tam ekran pencereyle örtüldüğünde (isOccluded == true):
                // Simülasyon arka planda kesintisiz çalışır (avlanma, ağ örme devam eder),
                // ancak GPU'ya render yapılmayarak %0 GPU kullanımı sağlanır.
                return new FrameRateDecision(IdleFrameInterval, ShouldUpdateEngine: true, ShouldRenderCanvas: false);
            }

            // Normal görünür durum: Ekranda görsel hareket varsa 60 FPS, yoksa 20 Hz (boşta bekleme)
            TimeSpan interval = hasVisualActivity ? ActiveFrameInterval : IdleFrameInterval;
            return new FrameRateDecision(interval, ShouldUpdateEngine: true, ShouldRenderCanvas: true);
        }
    }
}
