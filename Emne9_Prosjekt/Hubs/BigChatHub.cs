using Microsoft.AspNetCore.SignalR;


namespace Emne9_Prosjekt.Hubs;

public class BigChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        
        
        await Clients.All.SendAsync("UserConnected");
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        
        await Clients.All.SendAsync("UserDisconnected");
        await base.OnDisconnectedAsync(exception);
    }
    public async Task SendMessage(string message)
    {
        
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
}