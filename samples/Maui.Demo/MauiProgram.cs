using Bee.Api.Client;
using Bee.UI.Core;
using Bee.UI.Maui.Storage;

namespace Maui.Demo;

/// <summary>
/// MAUI host bootstrap. Builds the <see cref="MauiApp"/>, registers the root
/// <see cref="App"/>, and pre-configures <see cref="ApiClientInfo"/> so the
/// <c>ConnectionPage</c> can drive the JSON-RPC handshake against
/// <c>samples/QuickStart.Server</c> on first launch.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Default endpoint pointing at <c>samples/QuickStart.Server</c> (launchSettings
    /// pins it to <c>http://localhost:5050</c>). The connection page lets the user
    /// override it at runtime, so this is just the first-launch seed.
    /// </summary>
    public const string DefaultEndpoint = "http://localhost:5050/api";

    /// <summary>
    /// Builds the <see cref="MauiApp"/>. Called by each platform's bootstrap
    /// (e.g. <c>Platforms/MacCatalyst/AppDelegate.cs</c>).
    /// </summary>
    public static MauiApp CreateMauiApp()
    {
        // The framework's default ApiAuthorizationValidator only checks that the
        // ApiKey is present — the demo uses a placeholder string for visibility.
        ApiClientInfo.ApiKey = "maui-demo";
        ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;

        // Mac Catalyst (and iOS / Android / packaged Windows) sandbox the .app
        // bundle to read-only, so the default `EndpointStorage` — which calls
        // `ClientInfo.ClientSettings.Save()` next to the entry assembly — fails
        // with UnauthorizedAccessException. Swap in the Preferences-backed
        // storage from Bee.UI.Maui before any Connect button click reaches
        // ClientInfo.Initialize.
        ClientInfo.EndpointStorage = new MauiPreferenceEndpointStorage();

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        // No custom fonts: the demo keeps Resources/Fonts empty so the project
        // builds without any extra .ttf assets. Platform defaults are good enough
        // for a sample.

        return builder.Build();
    }
}
