using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IConnectFourGameHubConnection : IAsyncDisposable
{
    HubConnection Connection { get; }
    
    // Asynkrone metoder for sending
    Task SendJoinGameAsync(Dictionary<string, int> board); // Sender JoinGame med brettet
    Task SendGameOverAsync(bool youWon); // Sender GameOver med resultat
    Task SendUpdateBoardAsync(string opponentId, string pos); // Sender oppdatering av brettet
    Task SwitchTurnAsync(); // Sender forespørsel om å bytte tur

    // Register metoder for håndtering av eventer (handlers)
    void RegisterWaitingForOpponentHandler(Func<Task> handler); // Når vi venter på motstander
    void RegisterStartGameHandler(Func<Dictionary<string, int>, bool, Task> handler); // Når spillet starter
    void RegisterUpdateBoardHandler(Func<string, Task> handler); // Når brettet oppdateres
    void RegisterGameOverHandler(Func<bool, Task> handler); // Når spillet er over
    void RegisterOpponentDisconnectedHandler(Func<Task> handler);
    
}