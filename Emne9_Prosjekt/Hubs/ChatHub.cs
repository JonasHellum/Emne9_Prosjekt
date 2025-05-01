using Emne9_Prosjekt.Hubs.HubServices.Interfaces;
using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Emne9_Prosjekt.Hubs;

public class ChatHub : Hub<IChatClientMethods>
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IChatService _chatService; // Bruker tjenesten

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
        
        // Prøver å tildele brukeren en gruppe
        var (groupName, partnerConnectionId) = _chatService.AssignUserToGroup(connectionId);

        if (groupName != null) //Dersom en match ble funnet /aka ny connection kommer
        {
            //Legg til bruker i gruppen og send beskjed om at de nå snakker med en annen
            await Groups.AddToGroupAsync(connectionId, groupName);
            await Clients.Caller.ReceiveMessage("System", "You are now chatting with a partner.");
            //Legg til andreparten i samme gruppe 
            await Groups.AddToGroupAsync(partnerConnectionId!, groupName);
            await Clients.Client(partnerConnectionId!).ReceiveMessage("System", "A new user has joined your chat.");
        }
        else
        {
            //Dersom ingen partner er tilgjengelig, må bruker vente
            await Clients.Caller.ReceiveMessage("System", "Waiting for another user to join...");
        }

        await base.OnConnectedAsync();
    }
    
    /// <summary>
    /// Kalles når en klient kobler fra.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var username = Context.User?.Identity?.Name ?? "Guest";
        var connectionId = Context.ConnectionId;
        
        // Forsøker å fjerne brukeren fra en gruppe
        if (_chatService.TryRemoveUser(connectionId, out var groupName))
        {
            await Groups.RemoveFromGroupAsync(connectionId, groupName!);
            _logger.LogInformation($"User {connectionId} disconnected from {groupName}");
            await Clients.Groups(groupName!).NotifyUserDisconnected(username);
        }

        await base.OnDisconnectedAsync(exception);
    }
    
    /// <summary>
    /// Håndterer sending av meldinger mellom brukere i samme gruppe.
    /// </summary>
    /// <param name="message">Meldingen som skal sendes</param>
    public async Task SendMessage(string message)
    {
        var username = Context.User?.Identity?.Name ?? "Guest";
        var connectionId = Context.ConnectionId;
        // Henter gruppen brukeren tilhører
        if (_chatService.GetUserGroup(connectionId) is { } groupName)
        {
            await Clients.Group(groupName).ReceiveMessage(username, message);
        }
    }
}