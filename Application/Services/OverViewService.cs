using Application.ClientWindow;
using Application.ClientWindow.UIHandlers;
using Domen.Models;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class OverViewService
    {
        private readonly GameClient _client;

        public OverViewService(GameClient client)
        {
            _client = client;
            _client.SetNickname("Yavisha");
        }

        public IEnumerable<OverviewItem> GetInfo()
        {
            return _client.Parser.OV.GetInfo();
        }

        public bool ClickOnObject(OverviewItem spaceObject)
        {
            _client.Emulators.ClickLB(spaceObject.Pos);
            return true;
        }

        public bool LockTargetByName(string targetName)
        {
            var target = _client.Parser.OV.GetInfo().Find(item => item.Name == targetName);
            if (target == null)
                return false;

            _client.Emulators.ClickLB(target.Pos);
            return true;
        }

        public bool LockTargets(IEnumerable<OverviewItem> targets)
        {
            _client.Emulators.LockTargets(targets);
            return true;
        }
    }
}
