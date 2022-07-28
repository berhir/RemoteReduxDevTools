using Microsoft.AspNetCore.SignalR;
using RemoteReduxDevTools.Shared.CallbackObjects;
using System.Collections.Concurrent;

namespace RemoteReduxDevTools.Remote.Server.Hubs;

public class ReduxDevToolsHub : Hub<IReduxDevToolsHubClient>
{
    private static ConcurrentDictionary<string, string> _clientsInSessions = new();
    private readonly IHubContext<ClientAppHub, IClientAppHubClient> _clientAppHubContext;

    public ReduxDevToolsHub(IHubContext<ClientAppHub, IClientAppHubClient> clientAppHubContext)
    {
        _clientAppHubContext = clientAppHubContext;
    }

    public async Task NewSession(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException(nameof(id));
        }

        var connectionId = Context.ConnectionId;
        if (_clientsInSessions.TryGetValue(connectionId, out var oldSessionId))
        {
            await Groups.RemoveFromGroupAsync(connectionId, oldSessionId);
            _clientsInSessions.Remove(connectionId, out var _);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, id);
        _clientsInSessions.TryAdd(connectionId, id);
    }

    public async Task OnJumpToState(JumpToStateCallback jumpToStateCallback)
    {
        if (!_clientsInSessions.TryGetValue(Context.ConnectionId, out var sessionId))
        {
            throw new Exception("Start new session first");
        }
        await _clientAppHubContext.Clients.Group(sessionId).OnJumpToState(jumpToStateCallback);
    }

    public async Task OnCommit()
    {
        if (!_clientsInSessions.TryGetValue(Context.ConnectionId, out var sessionId))
        {
            throw new Exception("Start new session first");
        }
        await _clientAppHubContext.Clients.Group(sessionId).OnCommit();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _clientsInSessions.Remove(Context.ConnectionId, out var _);
        return base.OnDisconnectedAsync(exception);
    }
}
