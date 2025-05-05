using Emne9_Prosjekt.Hubs.HubModels;
using Emne9_Prosjekt.Hubs.HubServices.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Emne9_Prosjekt.Hubs;

public class ForumHub : Hub
{
    private readonly IForumService _forumService;
    private readonly ILogger<ForumHub> _logger;

    public ForumHub(IForumService forumService, ILogger<ForumHub> logger)
    {
        _forumService = forumService;
        _logger = logger;
    }
    public override async Task OnConnectedAsync()
    {
        var allMessages = _forumService.GetAllMessages();
        await Clients.Caller.SendAsync("MessagesUpdated", allMessages);
        await base.OnConnectedAsync();
    }
    
    public async Task SendMessage(Message message)
    {
        _forumService.AddMessage(message);
        _logger.LogInformation($"Received message: {message.Title} - {message.Content}");
        var allMessages = _forumService.GetAllMessages();
        await Clients.All.SendAsync("MessagesUpdated", allMessages);
    }
    public async Task SendComment(Comment comment)
    {
        _forumService.AddComment(comment);
        _logger.LogInformation($"Received comment: {comment.Text} for message: {comment.MessageId}");
        var allMessages = _forumService.GetAllMessages();
        await Clients.All.SendAsync("MessagesUpdated", allMessages);
    }
}