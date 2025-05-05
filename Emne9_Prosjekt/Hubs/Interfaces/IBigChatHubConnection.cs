using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IBigChatHubConnection : IAsyncDisposable
{
    HubConnection Connection { get; }
    Task SendMessageAsync(string message);
    Task RegisterUsernameAsync(string username);
    void RegisterReceiveMessageHandler(Func<string, string, Task> handler);
    void RegisterUserConnectedHandler(Func<string, Task> handler);
    void RegisterUserDisconnectedHandler(Func<string, Task> handler);
}