using Microsoft.AspNetCore.SignalR;


namespace Emne9_Prosjekt.Hubs;

public class BigChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        
        var username = Context.User?.Identity?.Name ?? "Guest";
        await Clients.All.SendAsync("UserConnected", username);
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var username = Context.User?.Identity?.Name ?? "Guest";
        await Clients.All.SendAsync("UserDisconnected", username);
        await base.OnDisconnectedAsync(exception);
    }
    public async Task SendMessage(string message)
    {
        var username = Context.User?.Identity?.Name ?? "Guest";
        await Clients.All.SendAsync("ReceiveMessage", message, username);
    }
}