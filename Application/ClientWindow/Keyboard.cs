using Application.ClientWindow.UIHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.ClientWindow
{
    public class Keyboard
    {
        private IntPtr _hWnd;
        private readonly Func<int, short> _getAsyncKeyState;
        private readonly Action<int>? _buttonSender;

        public Keyboard(IntPtr hWnd, Func<int, short>? getAsyncKeyState = null, Action<int>? buttonSender = null)
        {
            _hWnd = hWnd;
            _getAsyncKeyState = getAsyncKeyState ?? WinApi.GetAsyncKeyState;
            _buttonSender = buttonSender;
        }

        public void PressButton(int Button)
        {
            KeyDown(Button);
            KeyUp(Button);
        }

        public bool IsAnyModifierKeyDown()
        {
            const short KeyDownMask = unchecked((short)0x8000);
            return IsKeyDown(WinApi.VirtualKeyShort.VK_CONTROL, KeyDownMask)
                || IsKeyDown(WinApi.VirtualKeyShort.VK_MENU, KeyDownMask)
                || IsKeyDown(WinApi.VirtualKeyShort.VK_SHIFT, KeyDownMask);
        }

        public bool TryPressButton(int button)
        {
            if (IsAnyModifierKeyDown())
                return false;

            if (_buttonSender != null)
                _buttonSender(button);
            else
                PressButton(button);

            return true;
        }

        private bool IsKeyDown(WinApi.VirtualKeyShort key, short keyDownMask)
        {
            return (_getAsyncKeyState((int)key) & keyDownMask) != 0;
        }

        public void KeyDown(int Button)
        {
            WinApi.PostMessage(_hWnd, (uint)WinApi.KeyboardMessages.WM_IME_KEYDOWN, Button, 0);
            Thread.Sleep(100);
        }

        public void KeyUp(int Button)
        {
            WinApi.PostMessage(_hWnd, (uint)WinApi.KeyboardMessages.WM_IME_KEYUP, Button, 0);
            Thread.Sleep(100);
        }
    }
}
