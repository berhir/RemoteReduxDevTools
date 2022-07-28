namespace RemoteReduxDevTools.Client;

public class ActionInfo
{
#pragma warning disable IDE1006 // Naming Styles
    public string type { get; }
#pragma warning restore IDE1006 // Naming Styles
    public object Payload { get; }

    public ActionInfo(object action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        type = $"{GetTypeDisplayName(action.GetType())}, {action.GetType().Namespace}";
        Payload = action;
    }

    public static string GetTypeDisplayName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        string name = type.GetGenericTypeDefinition().Name;
        name = name.Remove(name.IndexOf('`'));
        IEnumerable<string> genericTypes = type
            .GetGenericArguments()
            .Select(GetTypeDisplayName);
        return $"{name}<{string.Join(",", genericTypes)}>";
    }
}
