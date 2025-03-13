using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Extensions;

public static class SignalRHubServiceExtension
{
    public static IServiceCollection AddSignalRHubConnection(this IServiceCollection services, string hubPath)
    {
        services.AddScoped(sp =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            return new HubConnectionBuilder()
                .WithUrl(navigationManager.ToAbsoluteUri(hubPath))
                .WithAutomaticReconnect() // Automatisk reconnect ved frakobling
                .Build();
        });

        return services;
    }
}