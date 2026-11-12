namespace SpiderDesk.Core.Services
{
    public interface IStartupRegistrationService
    {
        bool IsEnabled();
        void SetEnabled(bool enable);
    }
}
