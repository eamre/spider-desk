namespace SpiderDesk.Core.Settings
{
    public interface ISettingsRepository
    {
        string? LastError { get; }
        UserSettings Load();
        bool Save(UserSettings settings);
    }
}
