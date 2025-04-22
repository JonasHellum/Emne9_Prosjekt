using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Connections;

public class BigChatHubConnection : IBigChatHubConnection
{
    public HubConnection Connection { get; }
    
    public BigChatHubConnection(NavigationManager navigationManager)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/bigchathub"))
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}