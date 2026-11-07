using Infrastructure.WsConnecting;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWsClients(this IServiceCollection services)
        {
            services.AddSingleton<WsClient>();

            return services;
        }
    }
}
