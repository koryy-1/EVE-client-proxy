using Application.ClientWindow.UIHandlers;
using Domen.dto;
using Domen.Models;

namespace Application.ClientWindow
{
    public class Client
    {
        public Parser Parser { get; private set; }
        public Emulators Emulators { get; private set; }
        public ClientParams ClientParams { get; private set; }
        public SearchingStatus Status { get; private set; }

        private string _nickname;

        public Client()
        {
            Status = new SearchingStatus();
        }

        public void SetNickname(string nickname)
        {
            _nickname = nickname;
            ClientParams = PreInitClientParams(nickname);
            Status = new SearchingStatus();
        }

        public bool IsEveClientOpen(string nickname)
        {
            return UIRootAddress.IsEveClientOpen(nickname);
        }

        public SearchingStatus CheckRootAddressActuality()
        {
            var isActual = IsActualRootAddress(ClientParams);
            if (isActual)
            {
                Status.IsActualRootAddress = true;
                return Status;
            }
            else
            {
                // както запретить использование Parsers и Emulators
                Status.IsActualRootAddress = false;
                return Status;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newParams"></param>
        /// <returns>success</returns>
        public bool SetClientParams(ClientParams newParams)
        {
            if (!IsActualRootAddress(newParams))
            {
                return false;
            }
            ClientParams = newParams;
            Parser = new Parser(newParams);
            Emulators = new Emulators(newParams.hWnd);

            return true;
        }

        public void StartSearchingRootAddress()
        {
            if (IsActualRootAddress(ClientParams))
            {
                return;
            }

            SetStatusStartSearching();
            var newClientParams = UIRootAddress.UpdateRootAddress(_nickname);
            SetStatusStopSearching();

            SetClientParams(newClientParams);
        }

        public void StopSearching()
        {
            // Логика для остановки поиска рут адреса
            // concelationToken.RequestCancell
            SetStatusInterruptSearching();
            throw new NotImplementedException();
        }

        // proc name / hwnd / pid
        private ClientParams PreInitClientParams(string nickname)
        {
            return UIRootAddress.PreInitClientParams(nickname);
        }

        public bool IsActualRootAddress(ClientParams clientParams)
        {
            return UIRootAddress.IsActualRootAddress(clientParams);
        }

        private void SetStatusStartSearching()
        {
            Status.IsSearching = true;
            Status.IsActualRootAddress = false;
        }

        private void SetStatusStopSearching()
        {
            Status.IsSearching = false;
            Status.IsActualRootAddress = true;
        }

        private void SetStatusInterruptSearching()
        {
            Status.IsSearching = false;
            Status.IsActualRootAddress = false;
        }

        //todo: mapping
        public ClientParamsDto ClientParams2Dto(ClientParams clientParams)
        {
            ClientParamsDto clientParamsDto = new ClientParamsDto()
            {
                hWnd = clientParams.hWnd.ToInt32(),
                ProcessId = clientParams.ProcessId,
                ProcessName = clientParams.ProcessName,
                RootAddress = clientParams.RootAddress,
            };
            return clientParamsDto;
        }

        public ClientParams Dto2ClientParams(ClientParamsDto clientParamsDto)
        {
            ClientParams clientParams = new ClientParams()
            {
                hWnd = new IntPtr(clientParamsDto.hWnd),
                ProcessId = clientParamsDto.ProcessId,
                ProcessName = clientParamsDto.ProcessName,
                RootAddress = clientParamsDto.RootAddress,
            };
            return clientParams;
        }
    }
}
