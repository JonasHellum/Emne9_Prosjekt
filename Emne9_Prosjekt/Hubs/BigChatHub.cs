using Microsoft.AspNetCore.SignalR;


namespace Emne9_Prosjekt.Hubs;

public class BigChatHub : Hub
{ 
    private static Dictionary<string, string> _connectedUsers = new();
    private readonly ILogger<BigChatHub> _logger;
    public BigChatHub(ILogger<BigChatHub> logger)
    {
        _logger = logger;
    }
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation($"User connected. ConnectionId: {connectionId}");
        
        // Hent brukernavnet fra HTTP-headeren
        var username = Context.GetHttpContext()?.Request.Headers["Username"].ToString() ?? "Guest";
        _logger.LogInformation($"Username: {username}");
        
        // Legg til brukeren i den statiske ordboken
        _connectedUsers[connectionId] = username;
        
        await Clients.All.SendAsync("UserConnected");
        await base.OnConnectedAsync();
    }
    public Task RegisterUsername(string username)
    {
        var connectionId = Context.ConnectionId;
        _connectedUsers[connectionId] = username;
        Console.WriteLine($"Username {username} registered for connection {connectionId}");
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var username = _connectedUsers[connectionId];
        _connectedUsers.Remove(connectionId);  // Fjern fra ordboken
        _logger.LogInformation($"User {username} disconnected.");
        await Clients.All.SendAsync("UserDisconnected");
        await base.OnDisconnectedAsync(exception);
    }
    public async Task SendMessage(string message)
    {
        var connectionId = Context.ConnectionId;

        // Hent brukernavnet fra ordboken
        var username = _connectedUsers.ContainsKey(connectionId) ? _connectedUsers[connectionId] : "Guest";
        await Clients.All.SendAsync("ReceiveMessage",username, message);
    }
}