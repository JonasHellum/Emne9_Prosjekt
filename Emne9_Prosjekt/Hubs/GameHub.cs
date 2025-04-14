using Emne9_Prosjekt.Hubs.HubServices.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Emne9_Prosjekt.Hubs;

public class GameHub : Hub
{
    private readonly IGameService _gameService;

    public GameHub(IGameService gameService)
    {
        _gameService = gameService;
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;

        // Add player to the game service
        _gameService.AddPlayer(connectionId);

        // Automatically join the game
        await JoinGame();

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        // Get player's group before removing
        var groupId = _gameService.GetPlayerGroup(connectionId);
        var isGameInProgress = _gameService.IsGameInProgress(groupId);

        // Remove player from the game service
        _gameService.RemovePlayer(connectionId);

        // Notify the other player in the group if applicable
        if (!string.IsNullOrEmpty(groupId))
        {
            if (isGameInProgress)
            {
                // If game was in progress, notify the other player they won
                await Clients.Group(groupId).SendAsync("OpponentDisconnectedDuringGame");
            }
            else
            {
                // If game was in setup, just notify the other player that opponent disconnected
                await Clients.Group(groupId).SendAsync("OpponentDisconnected");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGame()
    {
        var connectionId = Context.ConnectionId;

        if (_gameService.TryCreateOrJoinGroup(connectionId, out var groupId))
        {
            // Add to SignalR group
            await Groups.AddToGroupAsync(connectionId, groupId);

            // Notify the client they've joined a group
            await Clients.Caller.SendAsync("JoinedGroup", groupId);

            // If there are 2 players in the group, notify both that the game can start
            var opponentId = _gameService.GetOpponentId(connectionId);
            if (!string.IsNullOrEmpty(opponentId))
            {
                // Notify the existing player that a new opponent has joined
                await Clients.Client(opponentId).SendAsync("OpponentConnected");

                // Notify both players that the game is ready
                await Clients.Group(groupId).SendAsync("GameReady", groupId);
            }
            else
            {
                await Clients.Caller.SendAsync("WaitingForOpponent", "Waiting for an opponent to join...");
            }
        }
    }

    public async Task SetupComplete(Dictionary<string, int> board)
    {
        var connectionId = Context.ConnectionId;
        var groupId = _gameService.GetPlayerGroup(connectionId);

        if (string.IsNullOrEmpty(groupId))
        {
            return;
        }

        // Set player as ready and update their board
        _gameService.SetPlayerReady(connectionId, board);

        // Notify the other player in the group
        await Clients.OthersInGroup(groupId).SendAsync("OpponentSetupComplete");

        // Check if both players are ready
        if (_gameService.AreBothPlayersReady(groupId))
        {
            // Get player numbers
            var playerNumber = _gameService.GetPlayerNumber(connectionId);
            var opponentId = _gameService.GetOpponentId(connectionId);
            var opponentNumber = _gameService.GetPlayerNumber(opponentId);

            // Get player turn status
            bool isPlayerTurn = _gameService.IsPlayerTurn(connectionId);
            bool isOpponentTurn = _gameService.IsPlayerTurn(opponentId);

            // Send player number, board, and turn status to each player
            await Clients.Client(connectionId).SendAsync("GameStarted", playerNumber, _gameService.GetPlayerBoard(connectionId));
            await Clients.Client(opponentId).SendAsync("GameStarted", opponentNumber, _gameService.GetPlayerBoard(opponentId));

            // Send turn information
            await Clients.Client(connectionId).SendAsync("TurnUpdate", isPlayerTurn);
            await Clients.Client(opponentId).SendAsync("TurnUpdate", isOpponentTurn);
        }
    }

    public async Task ShootAtOpponent(string position)
    {
        var connectionId = Context.ConnectionId;
        var groupId = _gameService.GetPlayerGroup(connectionId);

        if (string.IsNullOrEmpty(groupId))
        {
            return;
        }

        var opponentId = _gameService.GetOpponentId(connectionId);
        if (string.IsNullOrEmpty(opponentId))
        {
            return;
        }

        // Process the shot and check if it was successful (it was the player's turn)
        bool shotProcessed = _gameService.ProcessShot(connectionId, position);

        if (shotProcessed)
        {
            // Update both players' boards
            await Clients.Client(connectionId).SendAsync("UpdateOpponentBoard", _gameService.GetOpponentBoard(connectionId));
            await Clients.Client(opponentId).SendAsync("UpdateBoard", _gameService.GetPlayerBoard(opponentId));

            // Notify players about turn change
            await Clients.Client(connectionId).SendAsync("TurnUpdate", false); // Not your turn anymore
            await Clients.Client(opponentId).SendAsync("TurnUpdate", true);   // Your turn now
        }
    }
}