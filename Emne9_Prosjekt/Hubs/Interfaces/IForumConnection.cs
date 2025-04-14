using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IForumConnection : IAsyncDisposable
{
    HubConnection Connection { get; }
}