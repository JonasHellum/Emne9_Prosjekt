using Emne9_Prosjekt.Hubs.HubServices.Interfaces;
using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Emne9_Prosjekt.Hubs;

public class ChatHub : Hub<IChatClientMethods>
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IChatService _chatService; // Bruker tjenesten
    private static Dictionary<string, string> _connectedUsers = new();

    public ChatHub(ILogger<ChatHub> logger, IChatService chatService)
    {
        _logger = logger;
        _chatService = chatService;
    }
    
    /// <summary>
    /// Kalles når en klient kobler til chatten.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation($"User connected. ConnectionId: {connectionId}");
        
        // Hent brukernavnet fra HTTP-headeren
        var username = Context.GetHttpContext()?.Request.Headers["Username"].ToString() ?? "Guest";
        _logger.LogInformation($"Username: {username}");
        
        // Legg til brukeren i den statiske ordboken
        _connectedUsers[connectionId] = username;
        
        // Prøver å tildele brukeren en gruppe
        var (groupName, partnerConnectionId) = _chatService.AssignUserToGroup(connectionId);

        if (groupName != null) //Dersom en match ble funnet /aka ny connection kommer
        {
            //Legg til bruker i gruppen og send beskjed om at de nå snakker med en annen
            await Groups.AddToGroupAsync(connectionId, groupName);
            await Clients.Caller.ReceiveMessage("System", "You are now playing with a partner.");
            //Legg til andreparten i samme gruppe 
            await Groups.AddToGroupAsync(partnerConnectionId!, groupName);
            await Clients.Client(partnerConnectionId!).ReceiveMessage("System", "You are now playing with a partner.");
        }
        
        await base.OnConnectedAsync();
    }
    public Task RegisterUsername(string username)
    {
        var connectionId = Context.ConnectionId;
        _connectedUsers[connectionId] = username;
        Console.WriteLine($"Username {username} registered for connection {connectionId}");
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Kalles når en klient kobler fra.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        // Fjern brukeren fra ordboken når de kobler fra
        if (_connectedUsers.ContainsKey(connectionId))
        {
            var username = _connectedUsers[connectionId];
            _connectedUsers.Remove(connectionId);  // Fjern fra ordboken
            _logger.LogInformation($"User {username} disconnected.");
            
            // Forsøker å fjerne brukeren fra en gruppe
            if (_chatService.TryRemoveUser(connectionId, out var groupName))
            {
                await Groups.RemoveFromGroupAsync(connectionId, groupName!);
                await Clients.Groups(groupName!).NotifyUserDisconnected(username);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Håndterer sending av meldinger mellom brukere i samme gruppe.
    /// </summary>
    /// <param name="message">Meldingen som skal sendes</param>
    public async Task SendMessage(string message)
    {
        var connectionId = Context.ConnectionId;

        // Hent brukernavnet fra ordboken
        var username = _connectedUsers.ContainsKey(connectionId) ? _connectedUsers[connectionId] : "Guest";

        // Hent gruppen brukeren tilhører
        if (_chatService.GetUserGroup(connectionId) is { } groupName)
        {
            // Send meldingen til alle i gruppen
            await Clients.Group(groupName).ReceiveMessage(username, message);
        }
    }
}