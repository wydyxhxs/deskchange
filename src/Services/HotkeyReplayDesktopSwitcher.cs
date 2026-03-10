using System;
using System.Runtime.InteropServices;
using System.Threading;
using DeskChange.Interop;

namespace DeskChange.Services
{
    internal sealed class HotkeyReplayDesktopSwitcher : IVirtualDesktopSwitcher
    {
        private const int MaxDesktopHops = 20;
        private const int StepDelayMilliseconds = 90;
        private const int PostResetDelayMilliseconds = 120;
        private const int ReleasePollMilliseconds = 25;
        private const int ReleaseTimeoutMilliseconds = 1000;
        private const int KeyTransitionDelayMilliseconds = 25;

        public void GoToDesktop1()
        {
            WaitForTriggerKeysToRelease(NativeMethods.VK_1, NativeMethods.VK_NUMPAD1);
            ReplayToLeftEdge();
        }

        public void GoToDesktop2()
        {
            WaitForTriggerKeysToRelease(NativeMethods.VK_2, NativeMethods.VK_NUMPAD2);
            ReplayToLeftEdge();
            Thread.Sleep(PostResetDelayMilliseconds);
            SendDesktopShortcut(NativeMethods.VK_RIGHT);
        }

        private static NativeMethods.INPUT CreateKeyInput(ushort virtualKey, bool keyUp)
        {
            uint flags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0U;
            uint scanCode = NativeMethods.MapVirtualKey(virtualKey, 0);

            if (RequiresExtendedFlag(virtualKey))
            {
                flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
            }

            NativeMethods.INPUT input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_KEYBOARD;
            input.U = new NativeMethods.InputUnion();
            input.U.ki = new NativeMethods.KEYBDINPUT();
            input.U.ki.wVk = virtualKey;
            input.U.ki.dwFlags = flags;
            input.U.ki.wScan = (ushort)scanCode;
            input.U.ki.time = 0;
            input.U.ki.dwExtraInfo = IntPtr.Zero;

            return input;
        }

        private static bool IsPressed(int virtualKey)
        {
            return (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        private static bool RequiresExtendedFlag(ushort virtualKey)
        {
            return virtualKey == NativeMethods.VK_LEFT
                || virtualKey == NativeMethods.VK_RIGHT
                || virtualKey == NativeMethods.VK_LWIN;
        }

        private static void ReplayToLeftEdge()
        {
            int hopIndex;

            for (hopIndex = 0; hopIndex < MaxDesktopHops; hopIndex++)
            {
                SendDesktopShortcut(NativeMethods.VK_LEFT);
                Thread.Sleep(StepDelayMilliseconds);
            }
        }

        private static void SendDesktopShortcut(ushort arrowKey)
        {
            SendSingleInput(CreateKeyInput(NativeMethods.VK_CONTROL, false));
            Thread.Sleep(KeyTransitionDelayMilliseconds);
            SendSingleInput(CreateKeyInput(NativeMethods.VK_LWIN, false));
            Thread.Sleep(KeyTransitionDelayMilliseconds);
            SendSingleInput(CreateKeyInput(arrowKey, false));
            Thread.Sleep(KeyTransitionDelayMilliseconds);
            SendSingleInput(CreateKeyInput(arrowKey, true));
            Thread.Sleep(KeyTransitionDelayMilliseconds);
            SendSingleInput(CreateKeyInput(NativeMethods.VK_LWIN, true));
            Thread.Sleep(KeyTransitionDelayMilliseconds);
            SendSingleInput(CreateKeyInput(NativeMethods.VK_CONTROL, true));
        }

        private static void WaitForTriggerKeysToRelease(params int[] triggerKeys)
        {
            int waited = 0;

            while (waited < ReleaseTimeoutMilliseconds)
            {
                if (!IsPressed(NativeMethods.VK_CONTROL)
                    && !IsPressed(NativeMethods.VK_LCONTROL)
                    && !IsPressed(NativeMethods.VK_RCONTROL)
                    && !IsPressed(NativeMethods.VK_MENU)
                    && !IsPressed(NativeMethods.VK_LMENU)
                    && !IsPressed(NativeMethods.VK_RMENU)
                    && !AnyPressed(triggerKeys))
                {
                    return;
                }

                Thread.Sleep(ReleasePollMilliseconds);
                waited += ReleasePollMilliseconds;
            }
        }

        private static bool AnyPressed(int[] virtualKeys)
        {
            int index;

            for (index = 0; index < virtualKeys.Length; index++)
            {
                if (IsPressed(virtualKeys[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SendSingleInput(NativeMethods.INPUT input)
        {
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[] { input };
            uint sentCount = NativeMethods.SendInput(
                1,
                inputs,
                Marshal.SizeOf(typeof(NativeMethods.INPUT)));

            if (sentCount != 1)
            {
                throw new InvalidOperationException("Failed to send keyboard input.");
            }
        }
    }
}
