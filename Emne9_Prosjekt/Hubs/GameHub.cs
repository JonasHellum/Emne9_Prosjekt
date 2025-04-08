using Emne9_Prosjekt.GameComponents;
using Emne9_Prosjekt.Services;
using Microsoft.AspNetCore.SignalR;

namespace Emne9_Prosjekt.Hubs;

public class GameHub : Hub
{
    private readonly GameService _gameService;
    private readonly BattleShipComponents _battleShipComponents;
    private readonly ILogger<GameHub> _logger;

    public GameHub(GameService gameService, BattleShipComponents battleShipComponents, ILogger<GameHub> logger)
    {
        _gameService = gameService;
        _battleShipComponents = battleShipComponents;
        _logger = logger;
    }

    // Håndterer når en spiller kobler seg til
    public override async Task OnConnectedAsync()
    {
        string connectionId = Context.ConnectionId;
        _logger.LogInformation("Ny spiller koblet til: {ConnectionId}", connectionId);

        // Tildel spiller til gruppe
        string groupName = _gameService.AssignPlayerToGroup(connectionId);
        await Groups.AddToGroupAsync(connectionId, groupName);

        // Opprett og lagre nytt brett
        var board = _battleShipComponents.GetBoard();
        _gameService.SaveBoard(connectionId, board);

        // Sjekk for motstander
        string? opponentId = _gameService.GetOpponent(connectionId);
        if (opponentId != null)
        {
            // Koble spillere sammen
            _gameService.SetOpponents(connectionId, opponentId);
            await Clients.Caller.SendAsync("OpponentConnected");
            await Clients.Client(opponentId).SendAsync("OpponentConnected");

            // Sjekk om motstanderen er klar
            if (_gameService.IsSetupCompleted(opponentId))
            {
                await Clients.Caller.SendAsync("OpponentSetupComplete");
            }
        }
        else
        {
            // Vent på motstander
            await Clients.Caller.SendAsync("WaitingForOpponent", "Venter på at en motstander skal koble til...");
        }

        await base.OnConnectedAsync();
    }

    // Håndterer når en spiller kobler seg fra
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string connectionId = Context.ConnectionId;
        _logger.LogInformation("Spiller koblet fra: {ConnectionId}", connectionId);

        // Sjekk om vi skal beholde setup
        bool keepSetup = _gameService.IsSetupCompleted(connectionId);

        // Fjern fra gruppe hvis ikke setup er fullført
        if (!keepSetup)
        {
            if (_gameService.TryGetPlayerGroup(connectionId, out string? groupName) && groupName != null)
            {
                await Groups.RemoveFromGroupAsync(connectionId, groupName);
            }
        }

        // Varsle motstander
        if (_gameService.TryGetPlayerOpponent(connectionId, out string? opponentId) && opponentId != null)
        {
            await Clients.Client(opponentId).SendAsync("OpponentDisconnected");
        }

        // Fjern spillerdata
        _gameService.RemovePlayer(connectionId, keepSetup);

        await base.OnDisconnectedAsync(exception);
    }

    // Håndterer skudd mot motstander
    public async Task ShootAtOpponent(string position)
    {
        string shooterId = Context.ConnectionId;
        _logger.LogInformation("Spiller {ShooterId} skyter på posisjon {Position}", shooterId, position);

        if (_gameService.TryGetPlayerOpponent(shooterId, out string? opponentId) && opponentId != null)
        {
            bool success = _gameService.ProcessShot(position, opponentId, shooterId);
            
            if (success)
            {
                if (_gameService.TryGetBoard(opponentId, out var opponentBoard) && opponentBoard != null)
                {
                    // Oppdater begge spilleres visning
                    await Clients.Client(opponentId).SendAsync("UpdateBoard", opponentBoard);
                    await Clients.Caller.SendAsync("UpdateOpponentBoard", opponentBoard);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("NotYourTurn");
            }
        }
    }

    // Håndterer når en spiller er ferdig med setup
    public async Task SetupComplete(Dictionary<string, int> board)
    {
        string connectionId = Context.ConnectionId;
        _logger.LogInformation("Spiller {ConnectionId} fullførte setup", connectionId);

        // Lagre setup
        _gameService.CompleteSetup(connectionId, board);

        // Sjekk motstander
        if (_gameService.TryGetPlayerOpponent(connectionId, out string? opponentId) && opponentId != null)
        {
            if (_gameService.IsSetupCompleted(opponentId))
            {
                // Begge spillere er klare, start spillet
                if (_gameService.TryGetBoard(connectionId, out var playerBoard) && 
                    _gameService.TryGetBoard(opponentId, out var opponentBoard) &&
                    playerBoard != null && opponentBoard != null)
                {
                    // Initialiser første tur
                    if (_gameService.TryGetPlayerGroup(connectionId, out string? groupName) && groupName != null)
                    {
                        _gameService.InitializeFirstTurn(groupName);
                    }

                    await Clients.Caller.SendAsync("GameStarted", 1, playerBoard);
                    await Clients.Client(opponentId).SendAsync("GameStarted", 2, opponentBoard);
                }
            }
            else
            {
                // Varsle motstander at setup er fullført
                await Clients.Client(opponentId).SendAsync("OpponentSetupComplete");
            }
        }
    }

    // Håndterer reconnect av spiller
    public async Task ReconnectPlayer(string connectionId)
    {
        _logger.LogInformation("Spiller {ConnectionId} prøver å koble til igjen", connectionId);

        if (_gameService.TryGetPlayerGroup(connectionId, out string? groupName) && groupName != null)
        {
            // Gjenopprett gruppetilkobling
            await Groups.AddToGroupAsync(connectionId, groupName);

            // Sjekk motstander
            if (_gameService.TryGetPlayerOpponent(connectionId, out string? opponentId) && opponentId != null)
            {
                // Gjenopprett motstandertilkobling
                await Clients.Caller.SendAsync("OpponentConnected");
                await Clients.Client(opponentId).SendAsync("OpponentConnected");

                // Sjekk om motstanderen er klar
                if (_gameService.IsSetupCompleted(opponentId))
                {
                    await Clients.Caller.SendAsync("OpponentSetupComplete");
                }
            }
        }
    }
}