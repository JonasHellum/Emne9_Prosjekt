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
        await base.OnConnectedAsync();
    }
    public async Task RegisterUsername(string username)
    {
        var connectionId = Context.ConnectionId;
        _connectedUsers[connectionId] = username;
        _logger.LogInformation($"Username {username} registered for connection {connectionId}");
        await Clients.All.SendAsync("UserConnected", username);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        if (_connectedUsers.TryGetValue(connectionId, out var username))
        {
            _connectedUsers.Remove(connectionId);
            _logger.LogInformation($"User {username} disconnected.");
            await Clients.All.SendAsync("UserDisconnected", username);
        }
        else
        {
            _logger.LogWarning($"Disconnected connection {connectionId} had no registered username.");
        }

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