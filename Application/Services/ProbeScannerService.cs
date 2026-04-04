using Application.ClientWindow;
using Domen.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ProbeScannerService
    {
        private readonly Client _client;

        public ProbeScannerService(Client client)
        {
            _client = client;
            _client.SetNickname("Yavisha");
        }

        public IEnumerable<ProbeScanItem> GetInfo()
        {
            return _client.Parser.PS.GetInfo();
        }

        public void WarpToAnomaly(ProbeScanItem probeScanItem)
        {
            _client.Emulators.ClickLB(probeScanItem.Pos);
        }
    }
}
