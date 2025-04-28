using Emne9_Prosjekt.Hubs.HubServices.Interfaces;
using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Emne9_Prosjekt.Hubs;

public class ConnectFourGameHub : Hub<IConnectFourGameHubClientMethods>
{
    private readonly IConnectFourGameService _connectGameService;
    public ConnectFourGameHub(IConnectFourGameService connectGameService)
    {
        _connectGameService = connectGameService;
    }
     public async Task JoinGame(Dictionary<string, int> board)
    {
        var connectionId = Context.ConnectionId;
        Console.WriteLine($"[Hub] {connectionId} prøver å bli med i spill.");
        // Sjekk om spilleren allerede er i et spill
        if (_connectGameService.IsGameInProgress(connectionId))
        {
            // Spilleren er allerede i et spill, ikke gjør noe
            Console.WriteLine($"[Hub] {connectionId} er allerede i et spill.");
            return;
        }
        
        var game = _connectGameService.JoinGame(connectionId, board);

        if (game == null)
        {
            // Venter på en motspiller
            Console.WriteLine($"[Hub] {connectionId} venter på motspiller.");
            await Clients.Caller.WaitingForOpponent();
        }
        else
        {
            // Spillere er matched! Send beskjed til begge
            Console.WriteLine(
                $"[Hub] {connectionId} matchet med {game.Player1.ConnectionId} og {game.Player2.ConnectionId}.");
            await Clients.Client(game.Player1.ConnectionId).StartGame(game.Player2.Board, isMyTurn: true);
            await Clients.Client(game.Player2.ConnectionId).StartGame(game.Player1.Board, isMyTurn: false);
        }
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        Console.WriteLine($"[Hub] {connectionId} koblet fra.");

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
        Console.WriteLine($"[Hub] GameOver fra {connectionId}: {(youLost ? "Tapte" : "Vant")}.");
        var game = _connectGameService.GetGameByConnection(connectionId);

        Console.WriteLine($"Mottok GameOver fra {connectionId}: {(youLost ? "Tapte" : "Vant")}");

        if (game == null)
        {
            Console.WriteLine($"[Hub] Ingen spill funnet for {connectionId}.");
            Console.WriteLine("Fant ikke spill for denne tilkoblingen");
            return;
        }

        // Send GameOver til motstanderen med motsatt resultat
        var opponentId = game.Player1.ConnectionId == connectionId ? game.Player2.ConnectionId : game.Player1.ConnectionId;
        Console.WriteLine($"Sender GameOver til {opponentId}: {(!youLost ? "Tapte" : "Vant")}");
        try
        {
            await Clients.Client(opponentId).GameOver(!youLost);
            Console.WriteLine("GameOver-melding sendt til motstanderen");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Feil ved sending av GameOver: {ex.Message}");
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