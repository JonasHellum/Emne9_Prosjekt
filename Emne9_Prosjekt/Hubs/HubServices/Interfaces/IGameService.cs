using Emne9_Prosjekt.Hubs.HubModels;

namespace Emne9_Prosjekt.Hubs.HubServices.Interfaces;

public interface IGameService
{
    GameSession? JoinGame(string connectionId, Dictionary<string, int> board);
    GameSession? GetGameByConnection(string connectionId);
    bool IsPlayerTurn(string connectionId);
    void SwitchTurn(string connectionId);
    string GetOpponentId(string connectionId);
    bool IsGameInProgress(string connectionId);
    void RemovePlayer(string connectionId);
}