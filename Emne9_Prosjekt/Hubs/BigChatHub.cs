using Microsoft.AspNetCore.SignalR;


namespace Emne9_Prosjekt.Hubs;

public class BigChatHub : Hub
{
    private static Dictionary<string, string> ConnectedUsers = new();

    
    public override async Task OnConnectedAsync()
    {
        var userName = Context.GetHttpContext()?.Request.Headers["Username"].ToString() ?? "Guest";

        await Clients.All.SendAsync("UserConnected", userName);
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.GetHttpContext()?.Request.Headers["Username"].ToString() ?? "Guest";

        await Clients.All.SendAsync("UserDisconnected", userName);
        await base.OnDisconnectedAsync(exception);
    }
    public async Task SendMessage(string message)
    {
        var connectionId = Context.ConnectionId;
        var userName = ConnectedUsers.GetValueOrDefault(connectionId, "Guest");

        await Clients.All.SendAsync("ReceiveMessage", userName, message);
    }
    
    
    public async Task SetUserName(string userName)
    {
        var connectionId = Context.ConnectionId;

        if (!string.IsNullOrEmpty(userName))
        {
            ConnectedUsers[connectionId] = userName;
            await Clients.All.SendAsync("UserConnected", userName); 
        }
    }
}