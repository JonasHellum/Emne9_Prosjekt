using Emne9_Prosjekt.Hubs.HubServices.Interfaces;
using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Emne9_Prosjekt.Hubs;

public class ConnectFourGameHub : Hub<IConnectFourGameHubClientMethods>
{
    private readonly IConnectFourGameService _connectGameService;
    private readonly ILogger<ConnectFourGameHub> _logger;

    public ConnectFourGameHub(IConnectFourGameService connectGameService, ILogger<ConnectFourGameHub> logger)
    {
        _connectGameService = connectGameService;
        _logger = logger;
    }
     public async Task JoinGame(Dictionary<string, int> board)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation($"User {connectionId} connected.");
        // Sjekk om spilleren allerede er i et spill
        if (_connectGameService.IsGameInProgress(connectionId))
        {
            // Spilleren er allerede i et spill, ikke gjør noe
            _logger.LogInformation($"User {connectionId} is already in a game.");
            return;
        }
        
        var game = _connectGameService.JoinGame(connectionId, board);

        if (game == null)
        {
            // Venter på en motspiller
            _logger.LogInformation($"User {connectionId} waiting for opponent.");
            await Clients.Caller.WaitingForOpponent();
        }
        else
        {
            // Spillere er matched! Send beskjed til begge
            _logger.LogInformation($"User {connectionId} matched with {game.Player1.ConnectionId} and {game.Player2.ConnectionId}.");
            await Clients.Client(game.Player1.ConnectionId).StartGame(game.Player2.Board, isMyTurn: true);
            await Clients.Client(game.Player2.ConnectionId).StartGame(game.Player1.Board, isMyTurn: false);
        }
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation($"User {connectionId} disconnected.");

        // Sjekk om spilleren er i et aktivt spill
        var isGameInProgress = _connectGameService.IsGameInProgress(connectionId);

        // Finn motstanderen før vi fjerner spilleren
        var opponentId = _connectGameService.GetOpponentId(connectionId);

        // Fjern spilleren fra GameService
        _connectGameService.RemovePlayer(connectionId);

        // Hvis spilleren var i et aktivt spill, informer motstanderen
        if (isGameInProgress && !string.IsNullOrEmpty(opponentId))
        {
            await Clients.Client(opponentId).GameOver(true);
        }

        await base.OnDisconnectedAsync(exception);
    }
    
    public async Task GameOver(bool youLost)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation($"[Hub] GameOver fra {connectionId}: {(youLost ? "Tapte" : "Vant")}.");
        var game = _connectGameService.GetGameByConnection(connectionId);

        
        _logger.LogInformation($"Mottok GameOver fra {connectionId}: {(youLost ? "Tapte" : "Vant")}");

        if (game == null)
        {
            _logger.LogInformation($"Fant ikke spill for {connectionId}.");
            return;
        }

        // Send GameOver til motstanderen med motsatt resultat
        var opponentId = game.Player1.ConnectionId == connectionId ? game.Player2.ConnectionId : game.Player1.ConnectionId;
        _logger.LogInformation($"Sender GameOver til {opponentId}: {(!youLost ? "Tapte" : "Vant")}");
        try
        {
            await Clients.Client(opponentId).GameOver(!youLost);
            _logger.LogInformation($"GameOver-melding sendt til {opponentId}.");
            
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Feil ved sending av GameOver: {ex.Message}");
        }
    }
    
    // Hent motstanderens connection ID
    public string GetOpponentId()
    {
        var connectionId = Context.ConnectionId;
        return _connectGameService.GetOpponentId(connectionId);
    }
    
    // Oppdater motstanderens brett
    public async Task UpdateOpponentBoard(string opponentId, string position)
    {
        if (string.IsNullOrEmpty(opponentId))
            return;
            
        await Clients.Client(opponentId).UpdateBoard(position);
    }
    
    // Bytt tur
    public void SwitchTurn()
    {
        _logger.LogInformation($"[Hub] Bytter tur.");
        var connectionId = Context.ConnectionId;
        _connectGameService.SwitchTurn(connectionId);
    }
    
    // Sjekk om det er spillerens tur
    public bool IsPlayerTurn()
    {
        var connectionId = Context.ConnectionId;
        return _connectGameService.IsPlayerTurn(connectionId);
    }
}