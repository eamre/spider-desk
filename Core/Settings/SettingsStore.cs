using System;

namespace SpiderDesk.Core.Settings
{
    /// <summary>
    /// Uygulamanın tek ayar kaynağı: açılışta ayarları bir kez yükler, her değişiklikten sonra kaydeder.
    /// Ayarları değiştirmek isteyen herkes bu sınıfın <see cref="Update"/> metodunu kullanır.
    /// </summary>
    public sealed class SettingsStore
    {
        private readonly ISettingsRepository _repository;

        public UserSettings Current { get; }

        public SettingsStore(ISettingsRepository repository)
        {
            _repository = repository;
            Current = repository.Load();
        }

        /// <summary>Verilen değişikliği ayarlara uygular ve hemen diske kaydeder.</summary>
        public void Update(Action<UserSettings> change)
        {
            change(Current);
            _repository.Save(Current);
        }
    }
}
