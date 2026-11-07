using Application.ClientWindow;
using Domen.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.ClientWindow.UIHandlers
{
    static class UIRootAddress
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static UIRootAddress()
        {
            LogManager.Setup().LoadConfigurationFromFile("NLog.config");
        }

        public static bool IsEveClientOpen(string nickname)
        {
            var hWnd = WinApi.FindWindow("trinityWindow", $"EVE - {nickname}");

            if (hWnd.ToInt32() == 0)
                return false;
            else
                return true;
        }

        public static ClientParams PreInitClientParams(string nickname)
        {
            ClientParams clientParams = new ClientParams();
            clientParams.ProcessName = $"EVE - {nickname}";
            clientParams.hWnd = WinApi.FindWindow("trinityWindow", clientParams.ProcessName);

            if (clientParams.hWnd.ToInt32() == 0)
            {
                Log.Error("failed to find HWND by window name, check nickname or if game is open");

                throw new Exception("failed to find HWND by window name, check nickname or if game is open");
            }

            uint uintprocessId = 0;
            WinApi.GetWindowThreadProcessId(clientParams.hWnd, out uintprocessId);
            clientParams.ProcessId = Convert.ToInt32(uintprocessId);
            
            Log.Debug("pid by hWnd {0}", uintprocessId);

            return clientParams;
        }

        public static bool IsActualRootAddress(ClientParams clientParams)
        {
            if (clientParams == null)
            {
                throw new Exception("config is null");
            }

            var hWnd = WinApi.FindWindow("trinityWindow", clientParams.ProcessName);

            if (hWnd.ToInt32() == 0)
            {
                throw new Exception("failed to find HWND by window name, check nickname or if game is open");
            }

            uint uintprocessId = 0;
            WinApi.GetWindowThreadProcessId(hWnd, out uintprocessId);

            if (clientParams.ProcessId != Convert.ToInt32(uintprocessId)
                || string.IsNullOrEmpty(clientParams.RootAddress)
                // if root address have value, try to get some info (current solar system as example)
                )
            {
                return false;
            }
            return true;
        }

        public static ClientParams UpdateRootAddress(string nickname, CancellationToken token)
        {
            ClientParams clientParams = new ClientParams();
            clientParams.ProcessName = $"EVE - {nickname}";
            clientParams.hWnd = WinApi.FindWindow("trinityWindow", clientParams.ProcessName);

            if (clientParams.hWnd.ToInt32() == 0)
            {
                Log.Error("failed to find HWND by window name, check nickname or if game is open");
                throw new Exception("failed to find HWND by window name, check nickname or if game is open");
            }

            uint uintprocessId = 0;
            WinApi.GetWindowThreadProcessId(clientParams.hWnd, out uintprocessId);

            Log.Debug("please wait...");

            clientParams.ProcessId = Convert.ToInt32(uintprocessId);
            clientParams.RootAddress = getUIRootAddressByProcessName(clientParams.ProcessId, token);

            if (string.IsNullOrEmpty(clientParams.RootAddress))
            {
                Log.Error("failed to find root address by pid or HWND");
                throw new Exception("failed to find root address by pid or HWND");
            }

            Log.Debug("new PID in file = {0}", clientParams.ProcessId);
            Log.Debug("new RootAddress in file = {0}", clientParams.RootAddress);

            return clientParams;
        }

        private static string getUIRootAddressByProcessName(int processId, CancellationToken token)
        {

            var (uiRootCandidatesAddresses, memoryReader) = ReadMemory.GetRootAddressesAndMemoryReader(string.Empty, processId, token);

            IImmutableList<UITreeNode> ReadUITrees() =>
                    uiRootCandidatesAddresses
                    .Select(uiTreeRoot => EveOnline64.ReadUITreeFromAddress(uiTreeRoot, memoryReader, 99, token))
                    .Where(uiTree => uiTree != null)
                    .ToImmutableList();

            var readUiTreesStopwatch = System.Diagnostics.Stopwatch.StartNew();

            var uiTrees = ReadUITrees();

            readUiTreesStopwatch.Stop();

            var uiTreesWithStats =
                uiTrees
                .Select(uiTree =>
                new
                {
                    uiTree = uiTree,
                    nodeCount = uiTree.EnumerateSelfAndDescendants().Count()
                })
                .OrderByDescending(uiTreeWithStats => uiTreeWithStats.nodeCount)
                .ToImmutableList();

            var largestUiTree =
                uiTreesWithStats
                .OrderByDescending(uiTreeWithStats => uiTreeWithStats.nodeCount)
                .FirstOrDefault().uiTree;

            if (largestUiTree != null)
            {
                var ReadyRootAddress = largestUiTree.pythonObjectAddress.ToString();
                return ReadyRootAddress;
            }
            else
            {
                Log.Warn("No largest UI tree.");
                return null;
            }
        }
    }
}
