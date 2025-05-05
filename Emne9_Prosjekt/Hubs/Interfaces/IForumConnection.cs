using Emne9_Prosjekt.Hubs.HubModels;
using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IForumConnection : IAsyncDisposable
{
    HubConnection Connection { get; }
    Task SendMessageAsync(Message message);
    Task SendCommentAsync(Comment comment);
    void RegisterMessagesUpdatedHandler(Func<List<Message>, Task> handler);
}