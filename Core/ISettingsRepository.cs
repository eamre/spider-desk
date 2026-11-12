namespace SpiderDesk.Core
{
    public interface ISettingsRepository
    {
        string? LastError { get; }
        UserSettings Load();
        bool Save(UserSettings settings);
    }
}
