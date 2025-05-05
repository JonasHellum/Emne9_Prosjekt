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
    public Task SendMessageAsync(string message)
    {
        return Connection.SendAsync("SendMessage", message);
    }

    public Task RegisterUsernameAsync(string username)
    {
        return Connection.SendAsync("RegisterUsername", username);
    }

    public void RegisterReceiveMessageHandler(Func<string, string, Task> handler)
    {
        Connection.On("ReceiveMessage", handler);
    }

    public void RegisterUserConnectedHandler(Func<string, Task> handler)
    {
        Connection.On("NotifyUserConnected", handler);
    }

    public void RegisterUserDisconnectedHandler(Func<string, Task> handler)
    {
        Connection.On("NotifyUserDisconnected", handler);
    }
}