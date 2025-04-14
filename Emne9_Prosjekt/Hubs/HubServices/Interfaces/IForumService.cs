using Emne9_Prosjekt.Hubs.HubModels;

namespace Emne9_Prosjekt.Hubs.HubServices.Interfaces;

public interface IForumService
{
    List<Message> GetAllMessages();
    void AddMessage(Message message);
    void AddComment(Comment comment);
}