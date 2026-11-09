using Application.ClientWindow.UIHandlers;
using Domen.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.ClientWindow
{
    public class Mouse
    {
        private IntPtr _hWnd;

        public Mouse(IntPtr hWnd)
        {
            _hWnd = hWnd;
        }

        public void ClickLB(Point point)
        {
            Move(point);
            PostMouse(WinApi.MouseMessages.WM_LBUTTONDOWN, (int)WinApi.VirtualKeyShort.LBUTTON, point);
            Thread.Sleep(50);
            PostMouse(WinApi.MouseMessages.WM_LBUTTONUP, 0, point);
        }

        public void ClickRB(Point point)
        {
            Move(point);
            PostMouse(WinApi.MouseMessages.WM_RBUTTONDOWN, (int)WinApi.VirtualKeyShort.RBUTTON, point);
            Thread.Sleep(50);
            PostMouse(WinApi.MouseMessages.WM_RBUTTONUP, 0, point);
        }

        // TODO: need to masking moving virt cursor?
        public void Move(Point point) => PostMouse(WinApi.MouseMessages.WM_MOUSEMOVE, 0, point);

        public void Drag(int downX, int downY, int upX, int upY)
        {
            var down = new Point { x = downX, y = downY };
            var up = new Point { x = upX, y = upY };
            Move(down);
            PostMouse(WinApi.MouseMessages.WM_LBUTTONDOWN, (int)WinApi.VirtualKeyShort.LBUTTON, down);
            Thread.Sleep(50);
            Move(up);
            PostMouse(WinApi.MouseMessages.WM_LBUTTONUP, 0, up);
        }

        private void PostMouse(WinApi.MouseMessages message, int buttonState, Point point)
        {
            if (!WinApi.PostMessage(_hWnd, (uint)message, buttonState, WinApi.MakeLParam(point.x, point.y)))
                throw new InvalidOperationException($"PostMessage({message}) failed; Win32 error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}.");
        }
    }
}
