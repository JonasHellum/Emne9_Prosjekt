using Microsoft.AspNetCore.SignalR.Client;

namespace Emne9_Prosjekt.Hubs.Interfaces;

public interface IBigChatHubConnection : IAsyncDisposable
{
    HubConnection Connection { get; }
}