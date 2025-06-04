using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace WeighingSystem.Helpers
{
    internal class KeyboardHelper
    {
        /// <summary>
        /// Получает символ, соответствующий нажатой клавише, с учетом раскладки клавиатуры и модификаторов.
        /// </summary>
        /// <param name="key">Нажатая клавиша.</param>
        /// <returns>Символ как строка или null, если символ не определен.</returns>
        public static string GetCharFromKey(Key key)
        {
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            bool isSuccess = NativeMethods.GetKeyboardState(keyboardState);

            if (!isSuccess)
                return null;

            uint scanCode = NativeMethods.MapVirtualKey((uint)virtualKey, 0);
            IntPtr layout = NativeMethods.GetKeyboardLayout(0); // Текущая раскладка клавиатуры

            StringBuilder stringBuilder = new StringBuilder(2);
            int result = NativeMethods.ToUnicodeEx(
                (uint)virtualKey,
                scanCode,
                keyboardState,
                stringBuilder,
                stringBuilder.Capacity,
                0,
                layout);

            if (result > 0)
            {
                return stringBuilder.ToString();
            }

            return null;
        }

        /// <summary>
        /// Внутренний класс для работы с WinAPI.
        /// </summary>
        internal static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool GetKeyboardState(byte[] lpKeyState);

            [DllImport("user32.dll")]
            public static extern uint MapVirtualKey(uint uCode, uint uMapType);

            [DllImport("user32.dll")]
            public static extern IntPtr GetKeyboardLayout(uint idThread);

            [DllImport("user32.dll")]
            public static extern int ToUnicodeEx(
                uint wVirtKey,
                uint wScanCode,
                byte[] lpKeyState,
                [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
                int cchBuff,
                uint wFlags,
                IntPtr dwhkl);
        }
    }
}
