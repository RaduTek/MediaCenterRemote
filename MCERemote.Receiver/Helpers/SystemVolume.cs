using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MCERemote.Receiver.Helpers
{
    internal class SystemVolume
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private const byte VK_VOLUME_MUTE = 0xAD;
        private const byte VK_VOLUME_DOWN = 0xAE;
        private const byte VK_VOLUME_UP = 0xAF;

        private const int KEYEVENTF_KEYDOWN = 0x0000; // Key down
        private const int KEYEVENTF_KEYUP = 0x0002;   // Key up

        /// <summary>
        /// Sends the Volume Mute key to the system.
        /// </summary>
        public static void Mute()
        {
            keybd_event(VK_VOLUME_MUTE, 0, KEYEVENTF_KEYDOWN, 0);
            keybd_event(VK_VOLUME_MUTE, 0, KEYEVENTF_KEYUP, 0);
        }

        /// <summary>
        /// Sends the Volume Down key to the system.
        /// </summary>
        public static void VolumeDown()
        {
            keybd_event(VK_VOLUME_DOWN, 0, KEYEVENTF_KEYDOWN, 0);
            keybd_event(VK_VOLUME_DOWN, 0, KEYEVENTF_KEYUP, 0);
        }

        /// <summary>
        /// Sends the Volume Up key to the system.
        /// </summary>
        public static void VolumeUp()
        {
            keybd_event(VK_VOLUME_UP, 0, KEYEVENTF_KEYDOWN, 0);
            keybd_event(VK_VOLUME_UP, 0, KEYEVENTF_KEYUP, 0);
        }
    }
}
