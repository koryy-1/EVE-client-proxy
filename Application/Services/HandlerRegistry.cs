using System.Text.Json;
using Application.ClientWindow;
using Domen.dto;
using Domen.Models;

namespace Application.Services
{
    public class HandlerRegistry
    {
        private readonly Dictionary<string, Func<JsonElement, Task<object>>> _handlers;
        public HandlerRegistry(
            GameClient gameClient,
            DroneService droneService,
            HudInterfaceService hudInterfaceService,
            InfoPanelService infoPanelService,
            InventoryService inventoryService,
            OverViewService overViewService,
            ProbeScannerService probeScannerService,
            SelectItemService selectItemService
        )
        {
            _handlers = new Dictionary<string, Func<JsonElement, Task<object>>>
            {
                ["GameClient.IsEveClientOpen"] = payload =>
                {
                    var nickname = payload.GetProperty("nickname").GetString();
                    return Task.FromResult<object>(gameClient.IsEveClientOpen(nickname));
                },
                ["GameClient.SetNickname"] = payload =>
                {
                    var nickname = payload.GetProperty("nickname").GetString();
                    gameClient.SetNickname(nickname);
                    return Task.FromResult<object>(true);
                },
                ["GameClient.GetClientParams"] = payload => Task.FromResult<object>(gameClient.ClientParams2Dto(gameClient.ClientParams)),
                ["GameClient.GetSearchingStatus"] = payload => Task.FromResult<object>(gameClient.SearchingStatus),
                ["GameClient.GetSearchTaskStatus"] = payload => Task.FromResult<object>(gameClient.GetSearchTaskStatus()),
                ["GameClient.CheckRootAddressActuality"] = payload => Task.FromResult<object>(gameClient.CheckRootAddressActuality()),
                ["GameClient.UpdateClientParams"] = payload =>
                {
                    var clientParamsDto = payload.GetProperty("clientParamsDto").Deserialize<ClientParamsDto>();
                    var clientParams = gameClient.Dto2ClientParams(clientParamsDto);
                    return Task.FromResult<object>(gameClient.SetClientParams(clientParams));
                },
                ["GameClient.StartSearch"] = payload => Task.FromResult<object>(gameClient.StartSearchingRootAddress()),
                ["GameClient.StopSearch"] = payload => Task.FromResult<object>(gameClient.StopSearching()),


                ["DroneService.GetDronesInfo"] = payload => Task.FromResult<object>(droneService.GetInfo()),
                ["DroneService.Launch"] = payload => Task.FromResult<object>(droneService.Launch()),
                ["DroneService.Engage"] = payload => Task.FromResult<object>(droneService.Engage()),
                ["DroneService.Scoop"] = payload => Task.FromResult<object>(droneService.Scoop()),


                ["HudInterfaceService.GetHudInfo"] = payload => Task.FromResult<object>(hudInterfaceService.GetInfo()),
                ["HudInterfaceService.GetShipHP"] = payload => Task.FromResult<object>(hudInterfaceService.GetShipHP()),
                ["HudInterfaceService.GetCurrentSpeed"] = payload => Task.FromResult<object>(hudInterfaceService.GetCurrentSpeed()),
                ["HudInterfaceService.GetAllModules"] = payload => Task.FromResult<object>(hudInterfaceService.GetAllModules()),
                ["HudInterfaceService.GetShipFlightMode"] = payload => Task.FromResult<object>(hudInterfaceService.GetShipFlightMode()),
                ["HudInterfaceService.GetCenterPos"] = payload => Task.FromResult<object>(hudInterfaceService.GetCenterPos()),
                ["HudInterfaceService.ShipStop"] = payload => Task.FromResult<object>(hudInterfaceService.ShipStop()),
                ["HudInterfaceService.SetFullSpeed"] = payload => Task.FromResult<object>(hudInterfaceService.SetFullSpeed()),
                ["HudInterfaceService.ToggleActivationModule"] = payload =>
                {
                    var moduleName = payload.GetProperty("moduleName").GetString();
                    return Task.FromResult<object>(hudInterfaceService.ToggleActivationModule(moduleName));
                },


                ["InfoPanelService.GetRoutePanel"] = payload => Task.FromResult<object>(infoPanelService.GetRoutePanel()),
                ["InfoPanelService.GetLocation"] = payload => Task.FromResult<object>(infoPanelService.GetLocation()),


                ["InventoryService.GetInventoryInfo"] = payload => Task.FromResult<object>(inventoryService.GetInfo()),
                ["InventoryService.IsContainerOpened"] = payload => Task.FromResult<object>(inventoryService.IsContainerOpened()),
                ["InventoryService.LootAll"] = payload => Task.FromResult<object>(inventoryService.LootAll()),


                ["OverViewService.GetOverViewInfo"] = payload => Task.FromResult<object>(overViewService.GetInfo()),
                ["OverViewService.ClickOnObject"] = payload =>
                {
                    var spaceObject = payload.GetProperty("spaceObject").Deserialize<OverviewItem>();
                    return Task.FromResult<object>(overViewService.ClickOnObject(spaceObject));
                },
                ["OverViewService.LockTargets"] = payload =>
                {
                    var targets = payload.GetProperty("targets").Deserialize<IEnumerable<OverviewItem>>();
                    return Task.FromResult<object>(overViewService.LockTargets(targets));
                },
                ["OverViewService.LockTargetByName"] = payload =>
                {
                    var targetName = payload.GetProperty("targetName").GetString();
                    return Task.FromResult<object>(overViewService.LockTargetByName(targetName));
                },


                ["ProbeScannerService.GetProbeScanResults"] = payload => Task.FromResult<object>(probeScannerService.GetInfo()),
                ["ProbeScannerService.WarpToAnomaly"] = payload =>
                {
                    var probeScanItem = payload.GetProperty("probeScanItem").Deserialize<ProbeScanItem>();
                    return Task.FromResult<object>(probeScannerService.WarpToAnomaly(probeScanItem));
                },


                ["SelectItemService.GetSelectItemInfo"] = payload => Task.FromResult<object>(selectItemService.GetInfo()),
                ["SelectItemService.ClickButton"] = payload =>
                {
                    var btnName = payload.GetProperty("btnName").GetString();
                    return Task.FromResult<object>(selectItemService.ClickButton(btnName));
                },
            };
        }

        public async Task<object> Invoke(string type, JsonElement payload)
        {
            if (!_handlers.TryGetValue(type, out var handler))
            {
                Console.WriteLine($"[WS] Unknown method: {type}");
                throw new Exception($"Unknown method {type}");
            }

            return await handler(payload);
        }
    }
}
