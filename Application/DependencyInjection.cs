using Application.ClientWindow;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton<GameClient>();
            services.AddSingleton<DroneService>();
            services.AddSingleton<HudInterfaceService>();
            services.AddSingleton<InfoPanelService>();
            services.AddSingleton<InventoryService>();
            services.AddSingleton<OverViewService>();
            services.AddSingleton<ProbeScannerService>();
            services.AddSingleton<SelectItemService>();

            return services;
        }
    }
}
