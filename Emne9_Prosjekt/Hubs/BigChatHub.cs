using Microsoft.AspNetCore.SignalR;


namespace Emne9_Prosjekt.Hubs;

public class BigChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userName = Context.User?.Identity?.Name ?? "Guest";
        await Clients.All.SendAsync("UserConnected", userName);
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.User?.Identity?.Name ?? "Guest";
        await Clients.All.SendAsync("UserDisconnected", userName);
        await base.OnDisconnectedAsync(exception);
    }
    public async Task SendMessage(string message)
    {
        var userName = Context.User?.Identity?.Name ?? "Guest";
        await Clients.All.SendAsync("ReceiveMessage", userName, message);
    }
}