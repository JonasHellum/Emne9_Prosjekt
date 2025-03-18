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
            
            var firstTurn = _gameService.GetCurrentTurn(gameId);
            await Clients.Client(firstTurn).SendAsync("YourTurn", "It's your turn");
        }
        else
        {
            await Clients.Caller.SendAsync("Waiting", "Waiting for another player to join the game");
        }
        await base.OnConnectedAsync();
    }
}