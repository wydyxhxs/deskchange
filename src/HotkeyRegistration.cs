using System;

namespace DeskChange
{
    internal sealed class HotkeyRegistration
    {
        public HotkeyRegistration(int id, int desktopIndex, HotkeyBinding binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException("binding");
            }

            if (binding.IsEmpty)
            {
                throw new ArgumentException("A hotkey binding is required.", "binding");
            }

            Id = id;
            DesktopIndex = desktopIndex;
            Binding = binding.Clone();
        }

        public int DesktopIndex { get; private set; }

        public HotkeyBinding Binding { get; private set; }

        public int Id { get; private set; }

        public HotkeyRegistration Clone()
        {
            return new HotkeyRegistration(Id, DesktopIndex, Binding);
        }
    }
}
