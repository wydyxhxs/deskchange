namespace DeskChange.Services
{
    internal interface IStartupRegistration
    {
        bool IsEnabled();

        void Enable(string exePath, bool startHidden);

        void Disable();
    }
}
