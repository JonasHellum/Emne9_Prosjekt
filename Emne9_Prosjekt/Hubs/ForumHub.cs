using Emne9_Prosjekt.Hubs.HubModels;
using Emne9_Prosjekt.Hubs.HubServices.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Emne9_Prosjekt.Hubs;

public class ForumHub : Hub
{
    private readonly IForumService _forumService;

    public ForumHub(IForumService forumService)
    {
        _forumService = forumService;
    }
    
    public async Task SendMessage(Message message)
    {
        _forumService.AddMessage(message);
        var allMessages = _forumService.GetAllMessages();
        await Clients.All.SendAsync("MessagesUpdated", allMessages);
    }
    public async Task SendComment(Comment comment)
    {
        _forumService.AddComment(comment);
        var allMessages = _forumService.GetAllMessages();
        await Clients.All.SendAsync("MessagesUpdated", allMessages);
    }
}