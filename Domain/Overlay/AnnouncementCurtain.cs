namespace SpiderDesk.Domain.Overlay
{
    /// <summary>
    /// Bir karede perdenin ne yapacağı.
    /// </summary>
    /// <param name="HideBackground">Ağları ve böcekleri gizle: bir uygulamanın üstünde yalnızca örümcek ve yazısı/notu görünsün.</param>
    /// <param name="RedrawEverything">Bütün katmanları hemen yeniden çiz (gizlenen ağlar eski hâliyle görünmesin diye).</param>
    /// <param name="RaiseToTop">Katmanı bütün pencerelerin önüne çıkar.</param>
    public readonly record struct CurtainStep(bool HideBackground, bool RedrawEverything, bool RaiseToTop);

    /// <summary>
    /// Örümcek bir duyuru (FOCUS/BREAK) yazarken ya da bir not gösterirken katmanın, kullanıcı tam ekran bir uygulamadayken bile
    /// öne çıkmasını yöneten saf kural sınıfı. Sıra önemlidir, tiyatrodaki perde gibi:
    /// <list type="number">
    /// <item>İlk karede sahne hazırlanır: gerekiyorsa ağlar gizlenir ve her şey yeniden çizilir. Katman hâlâ uygulamanın arkasındadır, kimse görmez.</item>
    /// <item>Bir sonraki karede perde açılır: katman öne çıkar. Aynı karede çıksaydı arkada bekleyen eski görüntü (ağlar, böcekler) bir an görünürdü.</item>
    /// </list>
    /// Uygulama duraklatılmışsa katman öne çıkmaz; yoksa yarım kalan yazı uygulamaların üstünde asılı kalırdı.
    /// WPF/Win32 bağımlılığı yoktur; her kare <see cref="Next"/> ile ilerletilir.
    /// </summary>
    public sealed class AnnouncementCurtain
    {
        private bool _wasAnnouncingLastFrame;

        /// <param name="isShowingAnnouncement">Örümcek bir duyuru yazıyor ya da bir not gösteriyor mu?</param>
        /// <param name="isPaused">Kullanıcı uygulamayı tepsiden duraklattı mı?</param>
        /// <param name="isDesktopCovered">Masaüstünü bir uygulama örtüyor mu? (Masaüstündeyken ağlar zaten görünür, gizlenmez.)</param>
        public CurtainStep Next(bool isShowingAnnouncement, bool isPaused, bool isDesktopCovered)
        {
            bool isAnnouncing = isShowingAnnouncement && !isPaused;

            var step = new CurtainStep(
                HideBackground: isAnnouncing && isDesktopCovered,
                RedrawEverything: isAnnouncing && !_wasAnnouncingLastFrame,
                RaiseToTop: isAnnouncing && _wasAnnouncingLastFrame);

            _wasAnnouncingLastFrame = isAnnouncing;
            return step;
        }
    }
}
