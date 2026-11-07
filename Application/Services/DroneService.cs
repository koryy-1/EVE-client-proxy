using Application.ClientWindow;
using Application.ClientWindow.UIHandlers;
using Domen.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class DroneService
    {
        private readonly GameClient _client;
        public DroneService(GameClient client)
        {
            _client = client;
            _client.SetNickname("Yavisha");
        }

        public bool Launch()
        {
            _client.Emulators.Keyboard.PressButton((int)WinApi.VirtualKeyShort.VK_G);
            return true;
        }

        public bool Engage()
        {
            _client.Emulators.Keyboard.PressButton((int)WinApi.VirtualKeyShort.VK_F);
            return true;
        }

        public bool Scoop()
        {
            _client.Emulators.Keyboard.PressButton((int)WinApi.VirtualKeyShort.VK_H);
            return true;
        }

        public IEnumerable<Drone> GetInfo()
        {
            return _client.Parser.Drones.GetInfo();
        }

        //todo: make method launch / engage / return for concrete drone
    }
}
