namespace SpiderDesk.Infrastructure.Platform
{
    /// <summary>Uygulamanın Windows açılışında kendiliğinden başlaması.</summary>
    public interface IStartupRegistrationService
    {
        /// <summary>Windows açılışta <b>bu</b> exe'yi çalıştıracaksa true döner.</summary>
        bool IsEnabled();

        /// <summary>Açılışta çalışmayı açar ya da kapatır. Kayıt zaten doğruysa hiçbir şey yazmaz.</summary>
        void SetEnabled(bool enable);
    }
}
