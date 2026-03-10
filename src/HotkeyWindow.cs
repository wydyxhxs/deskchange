using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DeskChange.Interop;

namespace DeskChange
{
    [Flags]
    internal enum HotkeyModifiers : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004
    }

    internal sealed class HotkeyPressedEventArgs : EventArgs
    {
        public HotkeyPressedEventArgs(int hotkeyId)
        {
            HotkeyId = hotkeyId;
        }

        public int HotkeyId { get; private set; }
    }

    internal sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        private List<HotkeyRegistration> _registeredHotkeys = new List<HotkeyRegistration>();
        private bool _disposed;

        public HotkeyWindow()
        {
            CreateParams createParams = new CreateParams();
            createParams.Caption = "DeskChangeHotkeyWindow";
            createParams.X = 0;
            createParams.Y = 0;
            createParams.Width = 0;
            createParams.Height = 0;
            CreateHandle(createParams);
        }

        public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;

        public bool TryReplaceHotkeys(
            IList<HotkeyRegistration> registrations,
            out HotkeyRegistration failedRegistration,
            out int errorCode)
        {
            if (registrations == null)
            {
                throw new ArgumentNullException("registrations");
            }

            failedRegistration = null;
            errorCode = 0;

            List<HotkeyRegistration> previousHotkeys = CloneHotkeys(_registeredHotkeys);
            UnregisterHotkeys(_registeredHotkeys);
            _registeredHotkeys.Clear();

            int index;

            for (index = 0; index < registrations.Count; index++)
            {
                HotkeyRegistration registration = registrations[index];

                if (registration == null)
                {
                    continue;
                }

                if (!NativeMethods.RegisterHotKey(
                    Handle,
                    registration.Id,
                    (uint)registration.Binding.Modifiers,
                    (uint)registration.Binding.Key))
                {
                    errorCode = Marshal.GetLastWin32Error();
                    failedRegistration = registration.Clone();
                    UnregisterHotkeys(_registeredHotkeys);
                    _registeredHotkeys.Clear();
                    RestoreHotkeys(previousHotkeys);
                    return false;
                }

                _registeredHotkeys.Add(registration.Clone());
            }

            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterAllHotkeys();

            if (Handle != IntPtr.Zero)
            {
                DestroyHandle();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                EventHandler<HotkeyPressedEventArgs> handler = HotkeyPressed;
                if (handler != null)
                {
                    handler(this, new HotkeyPressedEventArgs(m.WParam.ToInt32()));
                }

                return;
            }

            base.WndProc(ref m);
        }

        private void UnregisterAllHotkeys()
        {
            UnregisterHotkeys(_registeredHotkeys);
            _registeredHotkeys.Clear();
        }

        private static List<HotkeyRegistration> CloneHotkeys(IList<HotkeyRegistration> registrations)
        {
            List<HotkeyRegistration> copy = new List<HotkeyRegistration>();
            int index;

            for (index = 0; index < registrations.Count; index++)
            {
                if (registrations[index] != null)
                {
                    copy.Add(registrations[index].Clone());
                }
            }

            return copy;
        }

        private void RestoreHotkeys(IList<HotkeyRegistration> registrations)
        {
            int index;

            for (index = 0; index < registrations.Count; index++)
            {
                HotkeyRegistration registration = registrations[index];

                if (registration == null)
                {
                    continue;
                }

                if (NativeMethods.RegisterHotKey(
                    Handle,
                    registration.Id,
                    (uint)registration.Binding.Modifiers,
                    (uint)registration.Binding.Key))
                {
                    _registeredHotkeys.Add(registration.Clone());
                }
            }
        }

        private void UnregisterHotkeys(IList<HotkeyRegistration> registrations)
        {
            int index;

            for (index = 0; index < registrations.Count; index++)
            {
                HotkeyRegistration registration = registrations[index];

                if (registration != null && Handle != IntPtr.Zero)
                {
                    NativeMethods.UnregisterHotKey(Handle, registration.Id);
                }
            }
        }
    }
}
