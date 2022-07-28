using Fluxor;
using Fluxor.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteReduxDevTools.Shared;
using RemoteReduxDevTools.Shared.CallbackObjects;
using RemoteReduxDevTools.Shared.Serialization;
using System.Diagnostics;
using IDispatcher = Fluxor.IDispatcher;

namespace RemoteReduxDevTools.Client;

/// <summary>
/// Middleware for interacting with the Redux Devtools extension for Chrome
/// </summary>
internal sealed class RemoteReduxDevToolsMiddleware : Middleware
{
    private Task _tailTask = Task.CompletedTask;
    private SpinLock _spinLock = new SpinLock();
    private int _sequenceNumberOfCurrentState = 0;
    private int _sequenceNumberOfLatestState = 0;
    private readonly RemoteReduxDevToolsMiddlewareOptions _options;
    private IStore? _store;
    private readonly IJsonSerialization _jsonSerialization;
    private readonly HubConnection _hubConnection;

    /// <summary>
    /// Creates a new instance of the middleware
    /// </summary>
    public RemoteReduxDevToolsMiddleware(
        RemoteReduxDevToolsMiddlewareOptions options,
        IJsonSerialization? jsonSerialization = null)
    {
        _options = options;
        _jsonSerialization = jsonSerialization ?? new NewtonsoftJsonAdapter();

        if(options.RemoteReduxDevToolsUri == null || string.IsNullOrEmpty(options.RemoteReduxDevToolsSessionId))
        {
            throw new ArgumentException("RemoteReduxDevToolsUri and RemoteReduxDevToolsSessionId must not be null");
        }

        var builder = new HubConnectionBuilder()
            .WithUrl(options.RemoteReduxDevToolsUri);

        if(options.SignalRLoggingConfig != null)
        {
            builder.ConfigureLogging(options.SignalRLoggingConfig);
        };

        _hubConnection = builder.Build();

        _hubConnection.On<JumpToStateCallback>("OnJumpToState", OnJumpToState);
        _hubConnection.On("OnCommit", OnCommit);
    }

    /// <see cref="IMiddleware.InitializeAsync(IDispatcher, IStore)"/>
    public async override Task InitializeAsync(IDispatcher dispatcher, IStore store)
    {
        _store = store;
        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync("JoinSessionAsync", _options.RemoteReduxDevToolsSessionId, new RemoteReduxDevToolsOptions
        {
            Name = _options.Name,
            Latency = _options.Latency,
            MaximumHistoryLength = _options.MaximumHistoryLength,
            StackTraceEnabled = _options.StackTraceEnabled,
        });
        await _hubConnection.InvokeAsync("InitializeAsync", GetStateAsJson());
    }

    /// <see cref="IMiddleware.MayDispatchAction(object)"/>
    public override bool MayDispatchAction(object action) =>
        _sequenceNumberOfCurrentState == _sequenceNumberOfLatestState;

    /// <see cref="IMiddleware.AfterDispatch(object)"/>
    public override void AfterDispatch(object action)
    {
        string? stackTrace = null;
        int maxItems = _options.StackTraceLimit == 0 ? int.MaxValue : _options.StackTraceLimit;
        if (_options.StackTraceEnabled)
        {
            stackTrace =
                string.Join("\r\n",
                    new StackTrace(fNeedFileInfo: true)
                        .GetFrames()
                        .Select(x => $"at {x.GetMethod().DeclaringType.FullName}.{x.GetMethod().Name} ({x.GetFileName()}:{x.GetFileLineNumber()}:{x.GetFileColumnNumber()})")
                        .Where(x => _options.StackTraceFilterRegex?.IsMatch(x) != false)
                        .Take(maxItems));

            if(stackTrace != null)
            {
                stackTrace = _jsonSerialization.Serialize(stackTrace, typeof(string));
            }
        }
        _spinLock.ExecuteLocked(() =>
        {
            var state = GetStateAsJson();
            _tailTask = _tailTask
                .ContinueWith(_ => _hubConnection.InvokeAsync("DispatchAsync", _jsonSerialization.Serialize(new ActionInfo(action), typeof(ActionInfo)), state, stackTrace)).Unwrap();
        });

        // As actions can only be executed if not in a historical state (yes, "a" historical, pronounce your H!)
        // ensure the latest is incremented, and the current = latest
        _sequenceNumberOfLatestState++;
        _sequenceNumberOfCurrentState = _sequenceNumberOfLatestState;
    }

    private IDictionary<string, object> GetState()
    {
        var state = new Dictionary<string, object>();
        foreach (IFeature feature in _store.Features.Values.OrderBy(x => x.GetName()))
            state[feature.GetName()] = feature.GetState();
        return state;
    }

    private string GetStateAsJson()
    {
        var state = GetState();
        return _jsonSerialization.Serialize(state, state.GetType());
    }

    private async Task OnCommit()
    {
        // Wait for fire+forget state notifications to ReduxDevTools to dequeue
        await _tailTask.ConfigureAwait(false);

        await _hubConnection.InvokeAsync("InitializeAsync", GetStateAsJson());
        _sequenceNumberOfCurrentState = _sequenceNumberOfLatestState;
    }

    private async Task OnJumpToState(JumpToStateCallback callbackInfo)
    {
        // Wait for fire+forget state notifications to ReduxDevTools to dequeue
        await _tailTask.ConfigureAwait(false);

        _sequenceNumberOfCurrentState = callbackInfo.payload.actionId;
        using (_store.BeginInternalMiddlewareChange())
        {
            var newFeatureStates = _jsonSerialization.Deserialize<Dictionary<string, object>>(callbackInfo.state);
            foreach (KeyValuePair<string, object> newFeatureState in newFeatureStates)
            {
                // Get the feature with the given name
                if (!_store.Features.TryGetValue(newFeatureState.Key, out IFeature feature))
                    continue;

                object stronglyTypedFeatureState = _jsonSerialization
                    .Deserialize(
                        json: newFeatureState.Value.ToString(),
                        type: feature.GetStateType());

                // Now set the feature's state to the deserialized object
                feature.RestoreState(stronglyTypedFeatureState);
            }
        }
    }
}
