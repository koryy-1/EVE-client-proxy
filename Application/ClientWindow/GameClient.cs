using Application.ClientWindow.UIHandlers;
using Domen.dto;
using Domen.Enums;
using Domen.Models;

namespace Application.ClientWindow
{
    public class GameClient
    {
        public Parser Parser { get; private set; }
        public Emulators Emulators { get; private set; }
        public ClientParams ClientParams { get; private set; }
        public SearchingStatus SearchingStatus { get; private set; }

        private Task _searchTask;
        private CancellationTokenSource _cts;

        private string _nickname;

        public GameClient()
        {
            ClientParams = new();
        }

        public bool SetNickname(string nickname)
        {
            _nickname = nickname;
            // ClientParams = PreInitClientParams(nickname);
            SearchingStatus = SearchingStatus.Idle;
            return true;
        }

        public bool IsEveClientOpen(string nickname)
        {
            return UIRootAddress.IsEveClientOpen(nickname);
        }

        public bool CheckRootAddressActuality()
        {
            return IsActualRootAddress(ClientParams);
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

        public TaskStatus GetSearchTaskStatus()
        {
            if (_searchTask == null)
                throw new Exception("searchTask has not yet been created");
            return _searchTask.Status;
        }

        public bool StartSearchingRootAddress()
        {
            if (SearchingStatus == SearchingStatus.Running)
                return false;

            if (_searchTask != null && !_searchTask.IsCompleted)
                return false;

            if (IsActualRootAddress(ClientParams))
                return false;

            _cts = new CancellationTokenSource();

            SearchingStatus = SearchingStatus.Running;

            _searchTask = Task.Run(() => SearchLoop(_cts.Token));

            return true;
        }

        public void SearchLoop(CancellationToken token)
        {
            try
            {
                var newClientParams = UIRootAddress.UpdateRootAddress(_nickname, token);

                if (token.IsCancellationRequested)
                    return;

                SetClientParams(newClientParams);
                SearchingStatus = SearchingStatus.Completed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured while searching: {ex.Message}");
                SearchingStatus = SearchingStatus.Failed;
            }
        }

        public bool StopSearching()
        {
            if (SearchingStatus != SearchingStatus.Running)
                return false;

            _cts.Cancel();
            SearchingStatus = SearchingStatus.Interrupted;
            return true;
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
