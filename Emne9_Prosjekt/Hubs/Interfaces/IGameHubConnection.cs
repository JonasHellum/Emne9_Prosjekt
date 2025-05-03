using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IGameHubConnection : IAsyncDisposable
{
    HubConnection Connection { get; }
    Task SendGameOverAsync(bool youWon);
    Task SendUpdateShipStatusAsync(string shipName, bool isSunk); 
    Task SendUpdateShotAsync(string position, string shooterId);
    void RegisterStartGameHandler(Func<Dictionary<string, int>, bool, Task> handler);
    void RegisterUpdateShotHandler(Func<string, string, bool, Task> handler);
    void RegisterUpdateShipStatusHandler(Func<string, bool, Task> handler);
    void RegisterGameOverHandler(Func<bool, Task> handler);
    void RegisterOpponentDisconnectedHandler(Func<Task> handler);
    void RegisterWaitingForOpponentHandler(Func<Task> handler);
    Task SendJoinGameAsync(Dictionary<string, int> board);
}