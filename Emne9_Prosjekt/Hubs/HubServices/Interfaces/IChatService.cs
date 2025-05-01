namespace Emne9_Prosjekt.Hubs.HubServices.Interfaces;

public interface IChatService
{
    (string? groupName, string? partnerConnectionId) AssignUserToGroup(string connectionId);
    bool TryRemoveUser(string connectionId, out string? groupName);
    string? GetUserGroup(string connectionId);
    string? GetPartner(string connectionId);
    bool IsUserReconnected(string connectionId);
}