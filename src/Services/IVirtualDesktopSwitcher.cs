namespace DeskChange.Services
{
    internal interface IVirtualDesktopSwitcher
    {
        int CreateDesktop();

        int GetDesktopCount();

        int GetCurrentDesktopIndex();

        void RemoveDesktop(int desktopIndex);

        void SwitchToDesktop(int desktopIndex, bool enableAnimation);
    }
}
