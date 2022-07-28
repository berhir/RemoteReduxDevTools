namespace RemoteReduxDevTools.Shared;

public class RemoteReduxDevToolsOptions
{
    /// <summary>
    /// The name to display in the Redux Dev Tools window
    /// </summary>
    public string Name { get; set; } = "Fluxor";
    /// <summary>
    /// How often the Redux Dev Tools actions are updated.
    /// </summary>
    public TimeSpan Latency { get; set; } = TimeSpan.FromMilliseconds(50);
    /// <summary>
    /// How many actions to keep in the Redux Dev Tools history (maxAge setting).
    /// Default is 50.
    /// </summary>
    public ushort MaximumHistoryLength { get; set; } = 50;
    /// <summary>
    /// When enabled, the stack trace that led to the dispatch of an action will
    /// be displayed in Redux Dev Tools.
    /// </summary>
    public bool StackTraceEnabled { get; set; } = false;
}
