using Application.ClientWindow;
using Domen.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class InfoPanelService
    {
        private readonly GameClient _client;

        public InfoPanelService(GameClient client)
        {
            _client = client;
            _client.SetNickname("Yavisha");
        }

        public RoutePanel GetRoutePanel()
        {
            return _client.Parser.NavPanel.GetRoutePanel();
        }

        public LocationInfo GetLocation()
        {
            return _client.Parser.NavPanel.GetLocation();
        }
    }
}
