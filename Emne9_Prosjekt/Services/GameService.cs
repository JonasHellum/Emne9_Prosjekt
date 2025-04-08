using System.Collections.Concurrent;

// Bruker en trådsikker samling for å håndtere samtidige forespørsler

namespace Emne9_Prosjekt.Services;

public class GameService
{
    // En tråd-sikker kø som holder spillere som venter på en opponent
    private readonly ILogger<GameService> _logger;
    // Lagrer gruppe-tilkoblinger for spillere
    private readonly ConcurrentDictionary<string, string> _playerGroups = new();
    // Lagrer brett-tilstand for hver spiller
    private readonly ConcurrentDictionary<string, Dictionary<string, int>> _playerBoards = new();
    // Lagrer motstander-tilkoblinger
    private readonly ConcurrentDictionary<string, string> _playerOpponents = new();
    // Lagrer setup-status for hver spiller
    private readonly ConcurrentDictionary<string, bool> _setupCompleted = new();
    // Lagrer hvis tur det er for hver gruppe
    private readonly ConcurrentDictionary<string, string> _currentTurn = new();
    // Lagrer antall treff for hver spiller
    private readonly ConcurrentDictionary<string, int> _hitCounts = new();

    public GameService(ILogger<GameService> logger)
    {
        _logger = logger;
    }

    // Tildeler en spiller til en eksisterende gruppe eller oppretter ny
    public string AssignPlayerToGroup(string playerId)
    {
        _logger.LogInformation("Tildeler spiller {PlayerId} til gruppe", playerId);
        
        var openGame = _playerGroups.Values
            .GroupBy(g => g)
            .FirstOrDefault(g => g.Count() == 1);

        if (openGame != null)
        {
            _playerGroups[playerId] = openGame.Key;
            _logger.LogInformation("Spiller {PlayerId} koblet til eksisterende gruppe {GroupId}", playerId, openGame.Key);
            return openGame.Key;
        }

        string newGroup = Guid.NewGuid().ToString();
        _playerGroups[playerId] = newGroup;
        _logger.LogInformation("Opprettet ny gruppe {GroupId} for spiller {PlayerId}", newGroup, playerId);
        return newGroup;
    }

    // Finner motstanderen for en gitt spiller
    public string? GetOpponent(string playerId)
    {
        var opponent = _playerGroups
            .Where(kv => kv.Value == _playerGroups[playerId])
            .Select(kv => kv.Key)
            .FirstOrDefault(id => id != playerId);

        if (opponent != null)
        {
            _logger.LogInformation("Fant motstander {OpponentId} for spiller {PlayerId}", opponent, playerId);
        }
        return opponent;
    }

    // Lagrer et brett for en spiller
    public void SaveBoard(string playerId, Dictionary<string, int> board)
    {
        _playerBoards[playerId] = board;
        _logger.LogInformation("Lagret brett for spiller {PlayerId}", playerId);
    }

    // Kobler to spillere sammen som motstandere
    public void SetOpponents(string player1Id, string player2Id)
    {
        _playerOpponents[player1Id] = player2Id;
        _playerOpponents[player2Id] = player1Id;
        _logger.LogInformation("Koblet spiller {Player1Id} og {Player2Id} sammen som motstandere", player1Id, player2Id);
    }

    // Sjekker om en spiller har fullført setup
    public bool IsSetupCompleted(string playerId)
    {
        return _setupCompleted.TryGetValue(playerId, out bool completed) && completed;
    }

    // Markerer at en spiller har fullført setup
    public void CompleteSetup(string playerId, Dictionary<string, int> board)
    {
        _setupCompleted[playerId] = true;
        _playerBoards[playerId] = board;
        _logger.LogInformation("Spiller {PlayerId} fullførte setup", playerId);
    }

    // Henter brettet for en spiller
    public bool TryGetBoard(string playerId, out Dictionary<string, int>? board)
    {
        return _playerBoards.TryGetValue(playerId, out board);
    }

    // Sjekker om det er spilleren sin tur
    public bool IsPlayerTurn(string playerId)
    {
        if (_playerGroups.TryGetValue(playerId, out string? groupId) && groupId != null)
        {
            return _currentTurn.TryGetValue(groupId, out string? currentPlayer) && 
                   currentPlayer == playerId;
        }
        return false;
    }

    // Setter neste spiller sin tur
    private void SetNextTurn(string groupId)
    {
        if (_currentTurn.TryGetValue(groupId, out string? currentPlayer) && 
            _playerOpponents.TryGetValue(currentPlayer, out string? nextPlayer))
        {
            _currentTurn[groupId] = nextPlayer;
            _logger.LogInformation("Neste tur: {NextPlayer} i gruppe {GroupId}", nextPlayer, groupId);
        }
        else
        {
            _logger.LogWarning("Kunne ikke sette neste tur for gruppe {GroupId}", groupId);
        }
    }

    // Initialiserer første tur når spillet starter
    public void InitializeFirstTurn(string groupId)
    {
        var players = _playerGroups
            .Where(kv => kv.Value == groupId)
            .Select(kv => kv.Key)
            .ToList();
            
        if (players.Count == 2)
        {
            _currentTurn[groupId] = players[0];
            _logger.LogInformation("Første tur: {FirstPlayer} i gruppe {GroupId}", players[0], groupId);
        }
        else
        {
            _logger.LogWarning("Kunne ikke initialisere første tur for gruppe {GroupId}: {PlayerCount} spillere", groupId, players.Count);
        }
    }

    // Prosesserer et skudd mot en spiller
    public bool ProcessShot(string position, string targetPlayerId, string shooterId)
    {
        // Sjekk om det er skytteren sin tur
        if (!IsPlayerTurn(shooterId))
        {
            _logger.LogWarning("Spiller {ShooterId} prøvde å skyte utenfor sin tur", shooterId);
            return false;
        }

        if (_playerBoards.TryGetValue(targetPlayerId, out var board))
        {
            if (board.ContainsKey(position))
            {
                board[position] = board[position] == 1 ? -1 : -2;
                
                // Sett neste tur
                if (_playerGroups.TryGetValue(shooterId, out string? groupId) && groupId != null)
                {
                    SetNextTurn(groupId);
                }

                return true;
            }
        }
        _logger.LogWarning("Kunne ikke prosessere skudd på posisjon {Position} mot spiller {PlayerId}", position, targetPlayerId);
        return false;
    }

    
    public void RemovePlayer(string playerId, bool keepSetup = false)
    {
        if (!keepSetup)
        {
            _playerGroups.TryRemove(playerId, out _);
            _playerBoards.TryRemove(playerId, out _);
            _setupCompleted.TryRemove(playerId, out _);
            _logger.LogInformation("Fjernet all data for spiller {PlayerId}", playerId);
        }
        _playerOpponents.TryRemove(playerId, out _);
        _logger.LogInformation("Fjernet motstander-tilkobling for spiller {PlayerId}", playerId);
    }

    // Henter gruppen for en spiller
    public bool TryGetPlayerGroup(string playerId, out string? groupName)
    {
        return _playerGroups.TryGetValue(playerId, out groupName);
    }

    // Henter motstanderen for en spiller
    public bool TryGetPlayerOpponent(string playerId, out string? opponentId)
    {
        return _playerOpponents.TryGetValue(playerId, out opponentId);
    }
}
