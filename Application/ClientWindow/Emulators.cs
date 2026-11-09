using Application.ClientWindow.UIHandlers;
using Domen.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.ClientWindow
{
    public class Emulators
    {
        public Mouse Mouse { get; set; }
        public Keyboard Keyboard { get; set; }

        IntPtr _hWnd;
        private readonly object _lock = new object();

        //static public WinApi.Rect windowRect = new WinApi.Rect();



        public Emulators(IntPtr hwnd)
        {
            _hWnd = hwnd;
            Mouse = new Mouse(_hWnd);
            Keyboard = new Keyboard(_hWnd);

        }

        static Random r = new Random();

        public void LockTargets(IEnumerable<OverviewItem> targets)
        {
            lock (_lock)
            {
                Keyboard.KeyDown((int)WinApi.VirtualKeyShort.VK_CONTROL);
                try
                {
                    Thread.Sleep(500);
                    foreach (var target in targets)
                    {
                        ClickLBForLockTargets(target.Pos);
                    }
                }
                finally
                {
                    Keyboard.KeyUp((int)WinApi.VirtualKeyShort.VK_CONTROL);
                }
                Thread.Sleep(500);
            }
        }

        public void ClickLBForLockTargets(Point point)
        {
            point.x += r.Next(-2, 2);
            point.y += r.Next(-2, 2);

            Mouse.ClickLB(point);
        }

        public void ClickLB(Point point)
        {
            point.x += r.Next(-2, 2);
            point.y += r.Next(-2, 2);

            lock (_lock)
            {
                Mouse.ClickLB(point);
            }
        }

        public void ClickRB(Point point)
        {
            point.x += r.Next(-2, 2);
            point.y += r.Next(-2, 2);

            lock (_lock)
            {
                Mouse.ClickRB(point);
            }
        }

        public void Drag(int DownX, int DownY, int UpX, int UpY)
        {
            DownX += r.Next(-2, 2);
            DownY += r.Next(-2, 2);

            UpX += r.Next(-2, 2);
            UpY += r.Next(-2, 2);

            Mouse.Drag(DownX, DownY, UpX, UpY);
        }
    }
}
