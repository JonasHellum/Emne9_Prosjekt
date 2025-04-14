using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Connections;

public class GameHubConnection : IGameHubConnection
{
    public HubConnection Connection { get; }

    public GameHubConnection(NavigationManager navigationManager)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}