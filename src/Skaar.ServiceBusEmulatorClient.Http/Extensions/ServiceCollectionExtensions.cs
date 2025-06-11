using Microsoft.Extensions.Options;
using Skaar.ServiceBusEmulatorClient.Http.Configuration;

namespace Skaar.ServiceBusEmulatorClient.Http.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(svc => svc.GetRequiredService<IOptions<Settings>>().Value);
        services.AddSingleton<IClient, Client>();
        return services;
    }
}