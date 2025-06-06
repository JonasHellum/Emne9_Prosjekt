﻿using Emne9_Prosjekt.Hubs.HubServices.Interfaces;
using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Emne9_Prosjekt.Hubs;

public class GameHub : Hub<IGameHubClientMethods>
{
     private readonly IGameService _gameService;
     private readonly ILogger<GameHub> _logger;

     public GameHub(IGameService gameService, ILogger<GameHub> logger)
     {
         _gameService = gameService;
         _logger = logger;
     }

    public async Task JoinGame(Dictionary<string, int> board)
    {
        var connectionId = Context.ConnectionId;
        var game = _gameService.JoinGame(connectionId, board);

        if (game == null)
        {
            // Venter på en motspiller
            _logger.LogInformation($"User {connectionId} waiting for opponent.");
            await Clients.Caller.WaitingForOpponent();
        }
        else
        {
            _logger.LogInformation($"User {connectionId} matched with {game.Player1.ConnectionId} and {game.Player2.ConnectionId}.");
            // Spillere er matched! Send beskjed til begge
            await Clients.Client(game.Player1.ConnectionId).StartGame(game.Player2.Board, isMyTurn: true);
            await Clients.Client(game.Player2.ConnectionId).StartGame(game.Player1.Board, isMyTurn: false);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        // Sjekk om spilleren er i et aktivt spill
        var isGameInProgress = _gameService.IsGameInProgress(connectionId);

        // Finn motstanderen før vi fjerner spilleren
        var opponentId = _gameService.GetOpponentId(connectionId);

        // Fjern spilleren fra GameService
        _gameService.RemovePlayer(connectionId);

        // Hvis spilleren var i et aktivt spill, informer motstanderen om at de vant
        if (isGameInProgress && !string.IsNullOrEmpty(opponentId))
        {
            await Clients.Client(opponentId).GameOver(true); // Motstanderen vant
        }

        await base.OnDisconnectedAsync(exception);
    }
    public async Task UpdateShot(string position, string shooterId)
    {
        var connectionId = Context.ConnectionId;
        var game = _gameService.GetGameByConnection(connectionId);
        
        _logger.LogInformation($"Mottok skudd fra {connectionId} på posisjon {position}");

        if (game == null)
        {
           _logger.LogInformation("Fant ikke spill for denne tilkoblingen");
            return;
        }

        if (!_gameService.IsPlayerTurn(connectionId))
        {
            _logger.LogInformation("Ikke spillerens tur");
            return;
        }

        // Finn ut om skuddet traff et skip
        bool isHit = false;
        var targetPlayer = game.Player1.ConnectionId == connectionId ? game.Player2 : game.Player1;

        // Sjekk om posisjonen inneholder et skip (verdi 1)
        if (targetPlayer.Board.ContainsKey(position) && targetPlayer.Board[position] == 1)
        {
            isHit = true;
            _logger.LogInformation($"Treff på posisjon {position}");
        }
        else
        {
            _logger.LogInformation($"Bom på posisjon {position}");
            
        }

        // Send resultatet til begge spillere
        _logger.LogInformation($"Sender resultat til {game.Player1.ConnectionId} og {game.Player2.ConnectionId}");
        await Clients.Client(game.Player1.ConnectionId).UpdateShot(position, shooterId, isHit);
        await Clients.Client(game.Player2.ConnectionId).UpdateShot(position, shooterId, isHit);

        // Bytt tur
        _logger.LogInformation($"Bytter tur for {connectionId}");
        _gameService.SwitchTurn(connectionId);
    }

    public async Task UpdateShipStatus(string shipName, bool isSunk)
    {
        var connectionId = Context.ConnectionId;
        var game = _gameService.GetGameByConnection(connectionId);
        _logger.LogInformation($"Mottok skipsstatus fra {connectionId}: {shipName}, sunket: {isSunk}");

        if (game == null)
        {
            _logger.LogInformation("Fant ikke spill for denne tilkoblingen");
            return;
        }

        // Send skipsstatus til motstanderen
        var opponentId = game.Player1.ConnectionId == connectionId ? game.Player2.ConnectionId : game.Player1.ConnectionId;
        _logger.LogInformation($"Sender skipsstatus til {opponentId}");
        await Clients.Client(opponentId).UpdateShipStatus(shipName, isSunk);
    }

    public async Task GameOver(bool youLost)
    {
        var connectionId = Context.ConnectionId;
        var game = _gameService.GetGameByConnection(connectionId);
        
        _logger.LogInformation($"Mottok GameOver fra {connectionId}: {(youLost ? "Tapte" : "Vant")}");

        if (game == null)
        {
            _logger.LogInformation("Fant ikke spill for denne tilkoblingen");
            return;
        }

        // Send GameOver til motstanderen med motsatt resultat
        var opponentId = game.Player1.ConnectionId == connectionId ? game.Player2.ConnectionId : game.Player1.ConnectionId;
        _logger.LogInformation($"Sender GameOver til {opponentId}: {(!youLost ? "Tapte" : "Vant")}");
        try
        {
            await Clients.Client(opponentId).GameOver(!youLost);
            _logger.LogInformation($"GameOver-melding sendt til {opponentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Feil ved sending av GameOver: {ex.Message}");
        }
    }

    public async Task UpdateOpponentHit(string opponentId, string position)
    {
        if (string.IsNullOrEmpty(opponentId))
            return;
            
        await Clients.Client(opponentId).UpdateOpponentBoardHit(position);
    } //Denne brukes ikke foreløpig
}