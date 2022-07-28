using Fluxor;
using RemoteReduxDevTools.Client;

namespace FluxorMauiApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddFluxor(o =>
        {
            o.ScanAssemblies(typeof(MauiProgram).Assembly);
            o.WithLifetime(StoreLifetime.Singleton);
            o.UseRemoteReduxDevTools(devToolsOptions =>
            {
                devToolsOptions.RemoteReduxDevToolsUri = new Uri("https://remotereduxdevtools.azurewebsites.net/clientapphub");
                devToolsOptions.RemoteReduxDevToolsSessionId = "01925cb8-52db-452c-bc53-fb92ce12036d";
                devToolsOptions.Name = "Fluxor MAUI";
                //devToolsOptions.EnableStackTrace();
            });
        });

        var mauiApp = builder.Build();
        return mauiApp;
    }
}
