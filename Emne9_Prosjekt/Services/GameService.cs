using System.Collections.Concurrent;
using Emne9_Prosjekt.GameComponents;

// Bruker en trådsikker samling for å håndtere samtidige forespørsler

namespace Emne9_Prosjekt.Services;

public class GameService
{
    // En tråd-sikker kø som holder spillere som venter på en opponent
    private static readonly ConcurrentQueue<string> WaitingQueue = new ();

    // En tråd-sikker ordbok som lagrer hvilke spillere som er i hvilke spill
    private static readonly ConcurrentDictionary<string, string> PlayerGames = new ();

    // En tråd-sikker ordbok som lagrer hvem som er motstanderen til hver spiller
    private static readonly ConcurrentDictionary<string, string> PlayerOpponents = new ();
    //  Holder styr på hvert spillers BattleShipComponents-objekt
    private static readonly ConcurrentDictionary<string, BattleShipComponents> PlayerBoards = new();
    private static readonly ConcurrentDictionary<string, string> CurrentTurn = new(); // Hvem sin tur det er
    

    /// <summary>
    /// Tildeler en spiller til et spill. Dersom en annen spiller allerede venter, kobles de sammen.
    /// Hvis ikke, legges spilleren i ventekøen.
    /// </summary>
    /// <param name="connectionId">Tilkoblings-ID for spilleren</param>
    /// <returns>Spill-ID og motstanderens tilkoblings-ID hvis en match finnes, ellers null</returns>
    public (string? gameId, string? opponentId) AssignPlayerToGame(string connectionId)
    {
        lock (WaitingQueue) // Sikrer at flere tråder ikke kan endre køen samtidig
        {
            if (WaitingQueue.TryDequeue(out var opponentId)) // Sjekker om det allerede finnes en spiller som venter
            {
                var gameId = $"game_{Guid.NewGuid()}"; // Oppretter et unikt spill-ID

                // Lagre spill-ID for begge spillerne
                PlayerGames[connectionId] = gameId;
                PlayerGames[opponentId] = gameId;
                /* PlayerGames["Player1"] = "game_123";
                PlayerGames["Player2"] = "game_123";*/

                // Lagre hvem som er motstandere
                PlayerOpponents[connectionId] = opponentId;
                PlayerOpponents[opponentId] = connectionId;
                /*Sånn her
                PlayerOpponents["Player1"] = "Player2";
                PlayerOpponents["Player2"] = "Player1"; */
                // Opprett BattleShipComponents for begge spillerne
                PlayerBoards[connectionId] = new BattleShipComponents();
                PlayerBoards[opponentId] = new BattleShipComponents();
                
                CurrentTurn[gameId] = connectionId; //Første spiller starter
                // Returnerer spill-ID og motstanderens tilkoblings-ID
                return (gameId, opponentId);
            }

            // Hvis ingen motstander er tilgjengelig, legges spilleren i køen
            WaitingQueue.Enqueue(connectionId);
            return (null, null);
        }
    }

    public bool IsPlayerTurn(string gameId, string connectionId)
    {
        return CurrentTurn.TryGetValue(gameId, out var currentPlayer) && currentPlayer == connectionId;
    }

    public void SwitchTurn(string gameId)
    {
        if (!CurrentTurn.TryGetValue(gameId, out var currentPlayer)) return;
        var opponentId = PlayerOpponents[currentPlayer];
        CurrentTurn[gameId] = opponentId;
    }

    public string GetCurrentTurn(string gameId)
    {
        return CurrentTurn.TryGetValue(gameId, out var currentPlayer) ? currentPlayer : "";
    }

    /// <summary>
    /// Fjerner en spiller fra et spill ved frakobling.
    /// </summary>
    /// <param name="connectionId">Tilkoblings-ID for spilleren</param>
    /// <param name="gameId">Ut-param som returnerer spill-ID'en</param>
    /// <returns>True hvis spilleren ble funnet og fjernet, ellers false</returns>
    public bool RemovePlayer(string connectionId, out string? gameId)
    {
        if (PlayerGames.TryRemove(connectionId, out gameId)) // Forsøker å fjerne spilleren fra spillet
        {
            // Sjekker om spilleren hadde en motstander
            if (PlayerOpponents.TryRemove(connectionId, out var opponentId))
            {
                // Fjerner motstanderen fra spillet også
                PlayerGames.TryRemove(opponentId, out _);
                PlayerOpponents.TryRemove(opponentId, out _);
                PlayerBoards.TryRemove(connectionId, out _);
            }
            PlayerBoards.TryRemove(connectionId, out _);
            return true; // Spilleren ble fjernet
        }
        return false; // Spilleren var ikke registrert i noe spill
    }

    /// <summary>
    /// Henter spill-ID for en gitt spiller.
    /// </summary>
    /// <param name="connectionId">Tilkoblings-ID</param>
    /// <returns>Spill-ID eller null hvis spilleren ikke er i et spill</returns>
    public string? GetGameId(string connectionId)
        => PlayerGames.TryGetValue(connectionId, out var gameId) ? gameId : null;

    /// <summary>
    /// Henter motstanderen til en gitt spiller.
    /// </summary>
    /// <param name="connectionId">Tilkoblings-ID</param>
    /// <returns>Motstanderens tilkoblings-ID eller null hvis ingen motstander er tilordnet</returns>
    public string? GetOpponent(string connectionId)
        => PlayerOpponents.TryGetValue(connectionId, out var opponentId) ? opponentId : null;
    
    // Henter en spillers BattleShipComponents
    public BattleShipComponents? GetPlayerBoard(string connectionId)
    {
        return PlayerBoards.TryGetValue(connectionId, out var board) ? board : null;
    }
}
