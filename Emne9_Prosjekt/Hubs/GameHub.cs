using Emne9_Prosjekt.Services;
using Microsoft.AspNetCore.SignalR;

namespace Emne9_Prosjekt.Hubs;

public class GameHub : Hub
{
    private readonly ILogger<GameHub> _logger;
    private readonly GameService _gameService;

    public GameHub(ILogger<GameHub> logger, GameService gameService)
    {
        _logger = logger;
        _gameService = gameService;
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation($"User connected. ConnectionId: {connectionId}");
        var (gameId, opponentId) = _gameService.AssignPlayerToGame(connectionId);
        if (gameId != null && opponentId != null)
        {
            await Groups.AddToGroupAsync(connectionId, gameId);
            await Groups.AddToGroupAsync(opponentId, gameId);
            
            await Clients.Caller.SendAsync("GameStarted", "Welcome to the game!");
            await Clients.Client(opponentId).SendAsync("GameStarted", "Welcome to the game");
            
            await SendBoardUpdates(connectionId, opponentId);
            
            var firstTurn = _gameService.GetCurrentTurn(gameId);
            await Clients.Client(firstTurn).SendAsync("YourTurn", "It's your turn");
        }
        else
        {
            await Clients.Caller.SendAsync("Waiting", "Waiting for another player to join the game");
        }
        await base.OnConnectedAsync();
    }
    public async Task Shoot(string position)
    {
        var connectionId = Context.ConnectionId;
        var (gameId, opponentId) = _gameService.GetGameByPlayer(connectionId);
        if (gameId == null || opponentId == null) return;

        if (_gameService.GetCurrentTurn(gameId) != connectionId)
        {
            await Clients.Caller.SendAsync("NotYourTurn", "It's not your turn!");
            return;
        }

        _logger.LogInformation($"Player {connectionId} shot at {position}.");
    
        // Skyt på motstanderens brett
        var opponentBoard = _gameService.GetPlayerBoard(opponentId);
        opponentBoard?.ShootBoard(position);

        // Oppdater begge spillere
        await SendBoardUpdates(connectionId, opponentId);

        // Bytt tur
        _gameService.SwitchTurn(gameId);
        var nextPlayer = _gameService.GetCurrentTurn(gameId);
        await Clients.Client(nextPlayer).SendAsync("YourTurn", "It's your turn!");
    }

    private async Task SendBoardUpdates(string player1, string player2)
    {
        var player1Board = _gameService.GetPlayerBoard(player1);
        var player2Board = _gameService.GetPlayerBoard(player2);

        await Clients.Client(player1).SendAsync("UpdatePlayerBoard", player1Board?.GetBoard());
        await Clients.Client(player1).SendAsync("UpdateOpponentBoard", player2Board?.GetBoard());

        await Clients.Client(player2).SendAsync("UpdatePlayerBoard", player2Board?.GetBoard());
        await Clients.Client(player2).SendAsync("UpdateOpponentBoard", player1Board?.GetBoard());
    }
}