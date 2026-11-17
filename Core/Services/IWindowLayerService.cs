using System.Windows;

namespace SpiderDesk.Core.Services
{
    public interface IWindowLayerService
    {
        /// <summary>Kullanıcının "Pencerelerin Üstünde Göster" ayarı.</summary>
        bool AlwaysOnTop { get; set; }

        /// <summary>
        /// Bir duyuru (FOCUS/BREAK) yazılırken katmanı geçici olarak bütün pencerelerin üstüne alır.
        /// Kullanıcının <see cref="AlwaysOnTop"/> ayarını değiştirmez; tıklamalar yine arkadaki uygulamaya geçer.
        /// </summary>
        bool IsPresentingOnTop { get; set; }

        bool IsOverlayInteractive { get; }

        /// <summary>Katman şu an görünmediği için çizim duraklatılabilir mi?</summary>
        bool IsDesktopOccluded { get; }

        /// <summary>
        /// Masaüstü büyütülmüş ya da tam ekran bir pencerenin altında mı? Katman geçici olarak öndeyken de doğru değeri verir;
        /// böylece bir uygulamanın üstünde yazı yazılırken ağlar ve böcekler gizlenebilir.
        /// </summary>
        bool IsDesktopCovered { get; }

        void Attach(Window window);
        void EnforceZOrder();
    }
}
