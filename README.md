# RemoteReduxDevTools

Use the Redux Dev Tools from any .NET application (Console, WinUI, MAUI, etc.) on any device with an internet connection.

This tool is based on the work of <a href="https://github.com/mrpmorris">Peter Morris</a> in the <a href="https://github.com/mrpmorris/Fluxor">Fluxor</a> library.

A hosted version of the remote server is available here: https://remotereduxdevtools.azurewebsites.net/.

# Usage

Add the RemoteReduxDevTools.Client project to any project that uses Fluxor.
Configure the RemoteReduxDevTools middleware:
```csharp
services.AddFluxor(o =>
{
    o.ScanAssemblies(typeof(App).Assembly);
    o.UseRemoteReduxDevTools(devToolsOptions =>
    {
        devToolsOptions.RemoteReduxDevToolsUri = new Uri("https://remotereduxdevtools.azurewebsites.net/clientapphub");
        devToolsOptions.RemoteReduxDevToolsSessionId = "4098c93f-5b4f-4a78-ab82-fefa2e2d30c3";
        devToolsOptions.Name = "Fluxor Console App";
        //devToolsOptions.EnableStackTrace();
    });
});
```
