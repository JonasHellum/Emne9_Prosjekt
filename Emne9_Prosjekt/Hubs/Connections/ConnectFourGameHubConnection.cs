using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Connections;

public class ConnectFourGameHubConnection : IConnectFourGameHubConnection
{
    public HubConnection Connection { get; }
    public ConnectFourGameHubConnection(NavigationManager navigationManager)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/connectgamehub"))
            .WithAutomaticReconnect()
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}