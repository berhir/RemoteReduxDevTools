using Microsoft.AspNetCore.SignalR;
using RemoteReduxDevTools.Shared;
using System.Collections.Concurrent;

namespace RemoteReduxDevTools.Remote.Server.Hubs;

public class ClientAppHub : Hub<IClientAppHubClient>
{
    private static ConcurrentDictionary<string, string> _clientsInSessions = new();
    private readonly IHubContext<ReduxDevToolsHub, IReduxDevToolsHubClient> _devToolsHubContext;

    public ClientAppHub(IHubContext<ReduxDevToolsHub, IReduxDevToolsHubClient> devToolsHubContext)
    {
        _devToolsHubContext = devToolsHubContext;
    }

    public async Task JoinSessionAsync(string sessonId, RemoteReduxDevToolsOptions config)
    {
        if (string.IsNullOrEmpty(sessonId))
        {
            throw new ArgumentException(nameof(sessonId));
        }

        var connectionId = Context.ConnectionId;
        if (_clientsInSessions.TryGetValue(connectionId, out var oldSessionId))
        {
            await Groups.RemoveFromGroupAsync(connectionId, oldSessionId);
            _clientsInSessions.Remove(connectionId, out var _);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessonId);
        _clientsInSessions.TryAdd(connectionId, sessonId);

        await _devToolsHubContext.Clients.Group(sessonId).ConfigureAsync(config);
    }

    public async Task InitializeAsync(string stateJson)
    {
        if (!_clientsInSessions.TryGetValue(Context.ConnectionId, out var sessionId))
        {
            throw new Exception("Join session first");
        }
        await _devToolsHubContext.Clients.Group(sessionId).InitializeAsync(stateJson);
    }

    public async Task DispatchAsync(string actionJson, string stateJson, string stackTrace)
    {
        if (!_clientsInSessions.TryGetValue(Context.ConnectionId, out var sessionId))
        {
            throw new Exception("Join session first");
        }
        await _devToolsHubContext.Clients.Group(sessionId).DispatchAsync(actionJson, stateJson, stackTrace);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _clientsInSessions.Remove(Context.ConnectionId, out var _);
        return base.OnDisconnectedAsync(exception);
    }
}
