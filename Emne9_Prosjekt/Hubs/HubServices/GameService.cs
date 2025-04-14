using Emne9_Prosjekt.GameComponents;
using Emne9_Prosjekt.Hubs.HubModels;
using Emne9_Prosjekt.Hubs.HubServices.Interfaces;

// Bruker en trådsikker samling for å håndtere samtidige forespørsler

namespace Emne9_Prosjekt.Hubs.HubServices;

public class GameService : IGameService
{
    private readonly Dictionary<string,Player> _players = new();
    private readonly Dictionary<string, List<string>> _groups = new();
    
    public void AddPlayer(string connectionId)
    {
        if(!_players.ContainsKey(connectionId))
        {
            _players[connectionId] = new Player
            {
                ConnectionId = connectionId,
                PlayerComponents = new BattleShipComponents()
            };
            //Initialize player's gameBoard
            _players[connectionId].PlayerComponents.CreateBoard();
        }
    }

    public void RemovePlayer(string connectionId)
    {
        if(_players.TryGetValue(connectionId, out var player) && !string.IsNullOrEmpty(player.GroupId))
        {
            //Remove player from group
            if(_groups.TryGetValue(player.GroupId, out var groupPlayers))
            {
                //Check if game is in progress
                bool gameInProgress = IsGameInProgress(player.GroupId);
                //Remove player from group
                groupPlayers.Remove(connectionId);
                
                //If group is empty, remove group
                if(groupPlayers.Count == 0)
                {
                    _groups.Remove(player.GroupId);
                }
                //if the game wasn't in progress, but there's still one player in the group, reset
                //their opponent-related state
                else if (!gameInProgress && groupPlayers.Count == 1)
                {
                    var remainingPlayerId = groupPlayers[0];
                    if(_players.TryGetValue(remainingPlayerId, out var remainingPlayer))
                    {
                        //Keep the player's board setup, but reset their ready state, wait for new opponent
                        remainingPlayer.IsTurn = false;
                    }
                }
            }
        }
        //remove player from players list
        _players.Remove(connectionId);
    }

    public bool TryCreateOrJoinGroup(string connectionId, out string groupId)
    {
        groupId = string.Empty;
        if (!_players.ContainsKey(connectionId))
        {
            return false;
        }
        //Check if player is already in a group
        var player = _players[connectionId];
        if (!string.IsNullOrEmpty(player.GroupId))
        {
            groupId = player.GroupId;
            return true;
        }
        
        //find a group with only one player, or create a new one
        foreach (var group in _groups)
        {
            if(group.Value.Count < 2)
            {
                group.Value.Add(connectionId);
                player.GroupId = group.Key;
                groupId = group.Key;
                return true;
            }
        }
        //create a new group
        groupId = $"group_{Guid.NewGuid()}";
        _groups[groupId] = new List<string> {connectionId};
        player.GroupId = groupId;
        return true;
    }

    public string GetPlayerGroup(string connectionId)
    {
        return _players.TryGetValue(connectionId, out var player) ? player.GroupId : string.Empty;
    }

    public string GetOpponentId(string connectionId)
    {
        if(!_players.TryGetValue(connectionId, out var player) || string.IsNullOrEmpty(player.GroupId))
        {
            return string.Empty;
        }
        if(!_groups.TryGetValue(player.GroupId, out var groupPlayers))
        {
            return string.Empty;
        }
        return groupPlayers.FirstOrDefault(p => p != connectionId) ?? string.Empty;
    }

    public void SetPlayerReady(string connectionId, Dictionary<string, int> board)
    {
        if (_players.TryGetValue(connectionId, out var player))
        {
            player.IsReady = true;

            // Update the player's board with the provided board
            foreach (var cell in board)
            {
                if (player.PlayerComponents.GetBoard().ContainsKey(cell.Key))
                {
                    player.PlayerComponents.GetBoard()[cell.Key] = cell.Value;
                }
            }

            // Check if this is the first player to be ready in the group
            var groupId = player.GroupId;
            if (!string.IsNullOrEmpty(groupId) && _groups.TryGetValue(groupId, out var groupPlayers))
            {
                // Count how many players are ready
                int readyCount = 0;
                foreach (var playerId in groupPlayers)
                {
                    if (_players.TryGetValue(playerId, out var p) && p.IsReady)
                    {
                        readyCount++;
                    }
                }

                // If this is the second player to be ready, set player 1's turn to true
                if (readyCount == 2)
                {
                    // Player 1 (first in the group) goes first
                    var firstPlayerId = groupPlayers[0];
                    if (_players.TryGetValue(firstPlayerId, out var firstPlayer))
                    {
                        firstPlayer.IsTurn = true;
                    }
                }
            }
        }
    }

    public bool AreBothPlayersReady(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var groupPlayers) || groupPlayers.Count != 2)
        {
            return false;
        }

        return groupPlayers.All(playerId => _players.TryGetValue(playerId, out var player) && player.IsReady);
    }

    public bool ProcessShot(string shooterConnectionId, string targetPosition)
    {
        var opponentId = GetOpponentId(shooterConnectionId);
        if (string.IsNullOrEmpty(opponentId) ||
            !_players.TryGetValue(opponentId, out var opponent) ||
            !_players.TryGetValue(shooterConnectionId, out var shooter))
        {
            return false;
        }

        // Check if it's the shooter's turn
        if (!shooter.IsTurn)
        {
            return false;
        }

        // Process the shot on the opponent's board
        opponent.PlayerComponents.ShootBoard(targetPosition);

        // Update the shooter's opponent board view
        var opponentBoardValue = opponent.PlayerComponents.GetBoard()[targetPosition];
        shooter.PlayerComponents.GetOpponentBoard()[targetPosition] = opponentBoardValue;

        // Switch turns
        shooter.IsTurn = false;
        opponent.IsTurn = true;

        return true;
    }

    public Dictionary<string, int> GetPlayerBoard(string connectionId)
    {
        return _players.TryGetValue(connectionId, out var player)
            ? player.PlayerComponents.GetBoard()
            : new Dictionary<string, int>();
    }

    public Dictionary<string, int> GetOpponentBoard(string connectionId)
    {
        return _players.TryGetValue(connectionId, out var player)
            ? player.PlayerComponents.GetOpponentBoard()
            : new Dictionary<string, int>();
    }

    public int GetPlayerNumber(string connectionId)
    {
        if (!_players.TryGetValue(connectionId, out var player) || string.IsNullOrEmpty(player.GroupId))
        {
            return 0;
        }

        if (!_groups.TryGetValue(player.GroupId, out var groupPlayers))
        {
            return 0;
        }

        // First player in the group is player 1, second is player 2
        return groupPlayers.IndexOf(connectionId) + 1;
    }

    public bool IsPlayerTurn(string connectionId)
    {
        return _players.TryGetValue(connectionId, out var player) && player.IsTurn;
    }

    public bool IsGameInProgress(string groupId)
    {
        if (string.IsNullOrEmpty(groupId) || !_groups.TryGetValue(groupId, out var groupPlayers))
        {
            return false;
        }

        // Check if all players in the group are ready
        bool allReady = groupPlayers.Count == 2 &&
                        groupPlayers.All(playerId =>
                            _players.TryGetValue(playerId, out var player) && player.IsReady);

        // If all players are ready, at least one player should have IsTurn set to true
        bool gameStarted = groupPlayers.Any(playerId =>
            _players.TryGetValue(playerId, out var player) && player.IsTurn);

        return allReady && gameStarted;
    }
}