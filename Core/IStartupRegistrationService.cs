namespace SpiderDesk.Core
{
    public interface IStartupRegistrationService
    {
        bool IsEnabled();
        void SetEnabled(bool enable);
    }
}
