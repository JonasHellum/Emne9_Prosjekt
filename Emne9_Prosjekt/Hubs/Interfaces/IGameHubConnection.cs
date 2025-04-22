using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IGameHubConnection : IAsyncDisposable
{
    HubConnection Connection { get; }
}