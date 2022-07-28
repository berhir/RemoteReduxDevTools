using RemoteReduxDevTools.Shared;

namespace RemoteReduxDevTools.Remote.Server.Hubs
{
    public interface IReduxDevToolsHubClient
    {
        Task ConfigureAsync(RemoteReduxDevToolsOptions config);

        Task InitializeAsync(string stateJson);

        Task DispatchAsync(string actionJson, string stateJson, string stackTrace);
    }
}
