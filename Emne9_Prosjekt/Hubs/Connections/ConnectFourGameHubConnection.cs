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
    // Asynkrone metoder for sending
    public async Task SendJoinGameAsync(Dictionary<string, int> board)
    {
        await Connection.SendAsync("JoinGame", board);
    }

    public async Task SendGameOverAsync(bool youWon)
    {
        await Connection.SendAsync("GameOver", youWon);
    }

    public async Task SendUpdateBoardAsync(string opponentId, string pos)
    {
        await Connection.SendAsync("UpdateOpponentBoard",opponentId,pos);
    }

    public async Task SwitchTurnAsync()
    {
        await Connection.SendAsync("SwitchTurn");
    }

    // Registrere eventhandlers
    public void RegisterWaitingForOpponentHandler(Func<Task> handler)
    {
        Connection.On("WaitingForOpponent", handler);
    }

    public void RegisterStartGameHandler(Func<Dictionary<string, int>, bool, Task> handler)
    {
        Connection.On("StartGame", handler);
    }

    public void RegisterUpdateBoardHandler(Func<string, Task> handler)
    {
        Connection.On("UpdateBoard", handler);
    }

    public void RegisterGameOverHandler(Func<bool, Task> handler)
    {
        Connection.On("GameOver", handler);
    }

    public void RegisterOpponentDisconnectedHandler(Func<Task> handler)
    {
        Connection.On("OpponentDisconnected", handler);
    }
}