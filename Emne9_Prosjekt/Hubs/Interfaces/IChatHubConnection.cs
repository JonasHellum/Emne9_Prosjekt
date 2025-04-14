using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IChatHubConnection : IAsyncDisposable
{
    HubConnection Connection { get; }
}