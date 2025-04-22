namespace Emne9_Prosjekt.Hubs.HubServices.Interfaces;

public interface IGameService
{
    void AddPlayer(string connectionId);
    void RemovePlayer(string connectionId);
    bool TryCreateOrJoinGroup(string connectionId, out string groupId);
    string GetPlayerGroup(string connectionId);
    string GetOpponentId(string connectionId);
    void SetPlayerReady(string connectionId, Dictionary<string, int> board);
    bool AreBothPlayersReady(string groupId);
    bool ProcessShot(string shooterConnectionId, string targetPosition);
    Dictionary<string, int> GetPlayerBoard(string connectionId);
    Dictionary<string, int> GetOpponentBoard(string connectionId);
    int GetPlayerNumber(string connectionId);
    bool IsPlayerTurn(string connectionId);
    bool IsGameInProgress(string groupId);
}