namespace DeskChange.Services
{
    internal sealed class RegisteredHotkeys
    {
        public bool Desktop1Main { get; set; }

        public bool Desktop1Numpad { get; set; }

        public bool Desktop2Main { get; set; }

        public bool Desktop2Numpad { get; set; }

        public bool HasDesktop1
        {
            get { return Desktop1Main || Desktop1Numpad; }
        }

        public bool HasDesktop2
        {
            get { return Desktop2Main || Desktop2Numpad; }
        }
    }
}
