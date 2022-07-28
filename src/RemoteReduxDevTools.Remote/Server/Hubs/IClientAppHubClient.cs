using RemoteReduxDevTools.Shared.CallbackObjects;

namespace RemoteReduxDevTools.Remote.Server.Hubs
{
    public interface IClientAppHubClient
    {
        Task OnJumpToState(JumpToStateCallback jumpToStateCallback);

        Task OnCommit();
    }
}
