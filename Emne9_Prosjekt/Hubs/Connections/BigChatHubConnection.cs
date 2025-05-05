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
            .WithAutomaticReconnect()
            .Build();
    }
    public Task SendMessageAsync(string message) =>
        Connection.SendAsync("SendMessage", message);

    public Task RegisterUsernameAsync(string username) =>
        Connection.SendAsync("RegisterUsername", username);

    public void RegisterReceiveMessageHandler(Func<string, string, Task> handler) =>
        Connection.On("ReceiveMessage", handler);

    public void RegisterUserConnectedHandler(Func<string, Task> handler) =>
        Connection.On("UserConnected", handler);

    public void RegisterUserDisconnectedHandler(Func<string, Task> handler) =>
        Connection.On("UserDisconnected", handler);

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}