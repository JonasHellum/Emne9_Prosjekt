using Emne9_Prosjekt.Hubs.HubModels;
using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Connections;

public class ForumConnection : IForumConnection
{
    public HubConnection Connection { get; }
    
    public ForumConnection(NavigationManager navigationManager)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/forumHub"))
            .WithAutomaticReconnect()
            .Build();
    }
    public Task SendMessageAsync(Message message) =>
        Connection.SendAsync("SendMessage", message);

    public Task SendCommentAsync(Comment comment) =>
        Connection.SendAsync("SendComment", comment);

    public void RegisterMessagesUpdatedHandler(Func<List<Message>, Task> handler) =>
        Connection.On("MessagesUpdated", handler);

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}