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

        /// <summary>
        /// Verilen değişikliği ayarlara uygular ve hemen diske kaydeder. Kayıt başarısız olursa false döner;
        /// değişiklik bellekte kalır ve bir sonraki başarılı kayıtla (her kayıt bütün ayarları yazar) diske geçer.
        /// </summary>
        public bool Update(Action<UserSettings> change)
        {
            change(Current);
            return _repository.Save(Current);
        }
    }
}
