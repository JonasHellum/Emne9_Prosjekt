using Emne9_Prosjekt.Hubs.HubModels;
using Emne9_Prosjekt.Hubs.HubServices.Interfaces;

namespace Emne9_Prosjekt.Hubs.HubServices;

public class ConnectFourGameService : IConnectFourGameService
{
    private readonly Dictionary<string, PlayerSession> _waitingPlayers = new();
    private readonly Dictionary<string, GameSession> _games = new();

    public GameSession? JoinGame(string connectionId, Dictionary<string, int> board)
    {
        Console.WriteLine($"[Service] {connectionId} joiner spill.");
        var player = new PlayerSession
        {
            ConnectionId = connectionId,
            Board = board
        };

        if (_waitingPlayers.Count == 0)
        {
            _waitingPlayers[connectionId] = player;
            return null; // Venter på motstander
        }
        else
        {
            var opponentEntry = _waitingPlayers.First();
            _waitingPlayers.Remove(opponentEntry.Key);

            var opponent = opponentEntry.Value;

            var gameId = Guid.NewGuid().ToString();
            var game = new GameSession
            {
                GameId = gameId,
                Player1 = opponent,
                Player2 = player,
                CurrentTurnConnectionId = opponent.ConnectionId // La motstander (første inn) starte
            };

            _games[gameId] = game;
            return game;
        }
    }

    public GameSession? GetGameByConnection(string connectionId)
    {
        return _games.Values.FirstOrDefault(g =>
            g.Player1.ConnectionId == connectionId || g.Player2.ConnectionId == connectionId);
    }

    public bool IsPlayerTurn(string connectionId)
    {
        var game = GetGameByConnection(connectionId);
        if (game == null) return false;

        return game.CurrentTurnConnectionId == connectionId;
    }

    public void SwitchTurn(string connectionId)
    {
        var game = GetGameByConnection(connectionId);
        if (game == null) return;

        game.CurrentTurnConnectionId = game.Player1.ConnectionId == connectionId
            ? game.Player2.ConnectionId
            : game.Player1.ConnectionId;
    }

    public string GetOpponentId(string connectionId)
    {
        var game = GetGameByConnection(connectionId);
        if (game == null) return string.Empty;

        return game.Player1.ConnectionId == connectionId
            ? game.Player2.ConnectionId
            : game.Player1.ConnectionId;
    }

    public bool IsGameInProgress(string connectionId)
    {
        var game = GetGameByConnection(connectionId);
        return game != null;
    }

    public void RemovePlayer(string connectionId)
    {
        // Fjern fra ventende spillere hvis de er der
        _waitingPlayers.Remove(connectionId);

        // Fjern fra aktive spill
        var game = GetGameByConnection(connectionId);
        if (game != null)
        {
            _games.Remove(game.GameId);
        }
    }
}