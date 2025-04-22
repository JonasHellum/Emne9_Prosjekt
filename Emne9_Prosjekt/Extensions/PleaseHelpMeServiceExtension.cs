using Emne9_Prosjekt.Hubs;
using Emne9_Prosjekt.Hubs.Connections;
using Emne9_Prosjekt.Hubs.Interfaces;

namespace Emne9_Prosjekt.Extensions;

public static class PleaseHelpMeServiceExtension
{
    public static IServiceCollection AddSignalRHubs(this IServiceCollection services)
    {
        services.AddScoped<IChatHubConnection, ChatHubConnection>();
        services.AddScoped<IGameHubConnection, GameHubConnection>();
        services.AddScoped<IForumConnection, ForumConnection>();
        services.AddScoped<IBigChatHubConnection, BigChatHubConnection>(); 
        return services;
    }
}