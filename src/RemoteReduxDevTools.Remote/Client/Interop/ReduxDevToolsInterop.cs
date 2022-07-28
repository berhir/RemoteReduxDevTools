using Microsoft.JSInterop;
using RemoteReduxDevTools.Shared;
using RemoteReduxDevTools.Shared.CallbackObjects;
using RemoteReduxDevTools.Shared.Serialization;

namespace RemoteReduxDevTools.Remote.Client.Interop;

/// <summary>
/// Interop for dev tools
/// </summary>
internal sealed class ReduxDevToolsInterop : IDisposable
{
    public const string DevToolsCallbackId = "DevToolsCallback";
    public bool DevToolsBrowserPluginDetected { get; private set; }
    public Func<JumpToStateCallback, Task>? OnJumpToState { get; set; }
    public Func<Task>? OnCommit { get; set; }

    private const string FluxorDevToolsId = "__FluxorDevTools__";
    private const string FromJsDevToolsDetectedActionTypeName = "detected";
    private const string ToJsDispatchMethodName = "dispatch";
    private const string ToJsInitMethodName = "init";
    private const string ToJsTerminateMethodName = "terminate";
    private bool _disposed = false;
    private bool _isInitializing = false;
    private readonly IJSRuntime _jSRuntime;
    private readonly IJsonSerialization _jsonSerialization;
    private readonly DotNetObjectReference<ReduxDevToolsInterop> _dotNetRef;

    /// <summary>
    /// Creates an instance of the dev tools interop
    /// </summary>
    /// <param name="jsRuntime"></param>
    public ReduxDevToolsInterop(
        IJSRuntime jsRuntime,
        IJsonSerialization? jsonSerialization = null)
    {
        _jSRuntime = jsRuntime;
        _jsonSerialization = jsonSerialization ?? new NewtonsoftJsonAdapter();
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    internal async Task ConfigureAsync(RemoteReduxDevToolsOptions options)
    {
        await _jSRuntime.InvokeVoidAsync("eval", GetClientScripts(options));
    }

    internal async ValueTask InitializeAsync(string stateJson)
    {
        _isInitializing = true;
        try
        {
            await InvokeFluxorDevToolsMethodAsync<object>(ToJsInitMethodName, _dotNetRef, stateJson);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    internal async ValueTask TerminateAsync()
    {
        await InvokeFluxorDevToolsMethodAsync<object>(ToJsTerminateMethodName);
    }

    internal async Task<object> DispatchAsync(
        string actionJson,
        string stateJson,
        string? stackTrace)
    =>
        await InvokeFluxorDevToolsMethodAsync<object>(
            ToJsDispatchMethodName,
            actionJson, stateJson, stackTrace)
        .ConfigureAwait(false);

    /// <summary>
    /// Called back from ReduxDevTools
    /// </summary>
    /// <param name="messageAsJson"></param>
    [JSInvokable(DevToolsCallbackId)]
    public async Task DevToolsCallback(string messageAsJson)
    {
        if (string.IsNullOrWhiteSpace(messageAsJson))
            return;

        var message = _jsonSerialization.Deserialize<BaseCallbackObject>(messageAsJson);
        switch (message?.payload?.type)
        {
            case FromJsDevToolsDetectedActionTypeName:
                DevToolsBrowserPluginDetected = true;
                break;

            case "COMMIT":
                Func<Task>? commit = OnCommit;
                if (commit is not null)
                {
                    Task task = commit();
                    if (task is not null)
                        await task;
                }
                break;

            case "JUMP_TO_STATE":
            case "JUMP_TO_ACTION":
                Func<JumpToStateCallback, Task>? jumpToState = OnJumpToState;
                if (jumpToState is not null)
                {
                    var callbackInfo = _jsonSerialization.Deserialize<JumpToStateCallback>(messageAsJson);
                    if (callbackInfo is not null)
                    {
                        Task task = jumpToState(callbackInfo);
                        if (task is not null)
                            await task;
                    }
                }
                break;
        }
    }

    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _dotNetRef.Dispose();
            _disposed = true;
        }
    }

    private ValueTask<TResult> InvokeFluxorDevToolsMethodAsync<TResult>(string identifier, params object[] args)
    {
        if (!DevToolsBrowserPluginDetected && !_isInitializing)
            return new ValueTask<TResult>(default(TResult));

        string fullIdentifier = $"{FluxorDevToolsId}.{identifier}";
        return _jSRuntime.InvokeAsync<TResult>(fullIdentifier, args);
    }

    internal static string GetClientScripts(RemoteReduxDevToolsOptions options)
    {
        string optionsJson = BuildOptionsJson(options);

        return $@"
window.{FluxorDevToolsId} = new (function() {{
	const reduxDevTools = window.__REDUX_DEVTOOLS_EXTENSION__;
	this.{ToJsInitMethodName} = function() {{}};
	if (reduxDevTools !== undefined && reduxDevTools !== null) {{
		const fluxorDevTools = reduxDevTools.connect({{ {optionsJson} }});
		if (fluxorDevTools !== undefined && fluxorDevTools !== null) {{
			fluxorDevTools.subscribe((message) => {{ 
				if (window.fluxorDevToolsDotNetInterop) {{
					const messageAsJson = JSON.stringify(message);
					window.fluxorDevToolsDotNetInterop.invokeMethodAsync('{DevToolsCallbackId}', messageAsJson); 
				}}
			}});
		}}
		this.{ToJsInitMethodName} = function(dotNetCallbacks, state) {{
			window.fluxorDevToolsDotNetInterop = dotNetCallbacks;
			state = JSON.parse(state);
			fluxorDevTools.init(state);
			if (window.fluxorDevToolsDotNetInterop) {{
				// Notify Fluxor of the presence of the browser plugin
				const detectedMessage = {{
					payload: {{
						type: '{FromJsDevToolsDetectedActionTypeName}'
					}}
				}};
				const detectedMessageAsJson = JSON.stringify(detectedMessage);
				window.fluxorDevToolsDotNetInterop.invokeMethodAsync('{DevToolsCallbackId}', detectedMessageAsJson);
			}}
		}};
		this.{ToJsDispatchMethodName} = function(action, state, stackTrace) {{
			action = JSON.parse(action);
			state = JSON.parse(state);
			window.fluxorDevToolsDotNetInterop.stackTrace = stackTrace;
			fluxorDevTools.send(action, state);
		}};
		this.{ToJsTerminateMethodName} = function() {{
			reduxDevTools.disconnect();
            window.fluxorDevToolsDotNetInterop = undefined;
		}};
	}}
}})();
";
    }

    private static string BuildOptionsJson(RemoteReduxDevToolsOptions options)
    {
        var values = new List<string> {
            $"name:\"{options.Name}\"",
            $"maxAge:{options.MaximumHistoryLength}",
            $"latency:{options.Latency.TotalMilliseconds}"
        };
        if (options.StackTraceEnabled)
            values.Add("trace: function() { return JSON.parse(window.fluxorDevToolsDotNetInterop.stackTrace); }");
        return string.Join(",", values);
    }
}
