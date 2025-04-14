using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Connections;

public class ChatHubConnection : IChatHubConnection
{
    public HubConnection Connection { get; }

    public ChatHubConnection(NavigationManager navigationManager)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/chathub"))
            .WithAutomaticReconnect()
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}