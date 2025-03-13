namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IChatClientMethods
{
    Task ReceiveMessage(string user, string message);
    Task NotifyUserConnected(string user);
    Task NotifyUserDisconnected(string user);
}