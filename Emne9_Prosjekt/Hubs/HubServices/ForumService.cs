using Emne9_Prosjekt.Hubs.HubModels;
using Emne9_Prosjekt.Hubs.HubServices.Interfaces;

namespace Emne9_Prosjekt.Hubs.HubServices;

public class ForumService : IForumService
{
    private readonly List<Message> _messages = new();
    public List<Message> GetAllMessages()
    {
        return _messages.Select(m => new Message()
        {
            Id = m.Id,
            Title = m.Title,
            Content = m.Content,
            Comments = m.Comments.Select(c => new Comment()
            {
                Id = c.Id,
                MessageId = c.MessageId,
                Text = c.Text
            }).ToList()
        }).ToList();
    }

    public void AddMessage(Message message)
    {
        _messages.Add(message);
    }

    public void AddComment(Comment comment)
    {
        var message = _messages.FirstOrDefault(m => m.Id == comment.MessageId);
        message?.Comments.Add(comment);
    }
}