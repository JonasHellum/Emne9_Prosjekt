using Emne9_Prosjekt.Hubs.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Connections;

public class ForumConnection : IForumConnection
{
    public HubConnection Connection { get; }
    
    public ForumConnection(NavigationManager navigationManager)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/forumHub"))
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}