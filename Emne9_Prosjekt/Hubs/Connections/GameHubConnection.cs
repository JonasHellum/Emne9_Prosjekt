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
    public Task SendGameOverAsync(bool youWon) => Connection.SendAsync("GameOver", youWon);
    public async Task SendUpdateShipStatusAsync(string shipName, bool isSunk)
    {
        await Connection.SendAsync("UpdateShipStatus", shipName, isSunk);
    }
    public Task SendUpdateShotAsync(string position, string shooterId) => Connection.SendAsync("UpdateShot", position, shooterId);
    public void RegisterStartGameHandler(Func<Dictionary<string, int>, bool, Task> handler)
    {
        Connection.On("StartGame", handler);
    }
    public void RegisterUpdateShotHandler(Func<string, string, bool, Task> handler)
    {
        Connection.On("UpdateShot", handler);
    }
    public void RegisterUpdateShipStatusHandler(Func<string, bool, Task> handler)
    {
        Connection.On("UpdateShipStatus", handler);
    }
    public void RegisterGameOverHandler(Func<bool, Task> handler)
    {
        Connection.On("GameOver", handler);
    }
    public void RegisterOpponentDisconnectedHandler(Func<Task> handler)
    {
        Connection.On("OpponentDisconnected", handler);
    }
    public void RegisterWaitingForOpponentHandler(Func<Task> handler)
    {
        Connection.On("WaitingForOpponent", handler);
    }
    public Task SendJoinGameAsync(Dictionary<string, int> board) => Connection.SendAsync("JoinGame", board);
}