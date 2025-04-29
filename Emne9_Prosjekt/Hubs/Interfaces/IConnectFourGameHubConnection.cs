using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IConnectFourGameHubConnection : IAsyncDisposable
{
    HubConnection Connection { get; }
}