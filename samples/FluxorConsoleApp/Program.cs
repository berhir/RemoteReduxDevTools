// See https://aka.ms/new-console-template for more information
using Fluxor;
using FluxorConsoleApp;
using Microsoft.Extensions.DependencyInjection;
using RemoteReduxDevTools.Client;

Console.WriteLine("Hello, World!");
var services = new ServiceCollection();
services.AddScoped<App>();
services.AddFluxor(o =>
{
    o.ScanAssemblies(typeof(App).Assembly);
    o.UseRemoteReduxDevTools(devToolsOptions =>
    {
        devToolsOptions.RemoteReduxDevToolsUri = new Uri("https://remotereduxdevtools.azurewebsites.net/clientapphub");
        devToolsOptions.RemoteReduxDevToolsSessionId = "5bfda23e-4b46-4499-aa49-88911cfec403";
        devToolsOptions.Name = "Fluxor Console";
        //devToolsOptions.EnableStackTrace();
    });
});
IServiceProvider serviceProvider = services.BuildServiceProvider();

var app = serviceProvider.GetRequiredService<App>();
await app.RunAsync();
