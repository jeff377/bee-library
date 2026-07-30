using Avalonia;
using Bee.Api.Client;
using Bee.Northwind.UI;
using Bee.UI.Avalonia.Storage;
using Bee.UI.Core;

namespace Bee.Northwind.Desktop;

/// <summary>
/// Desktop head — the thin process entry point. Wires the Bee client-side singletons
/// (<see cref="ApiClientInfo"/> + <see cref="ClientInfo.EndpointStorage"/>) before any
/// Avalonia control instantiates, then hands control to the classic-desktop lifetime
/// hosting the shared <see cref="App"/> from <c>Bee.Northwind.UI</c>.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Application entry point. <c>STAThread</c> is required by Windows for Avalonia to
    /// drive native dialogs / drag-drop / clipboard.
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        // Configure the Bee client singletons before any control or VM runs. EndpointStorage
        // must point at a writable per-user folder; FileEndpointStorage handles that for
        // unpackaged Avalonia hosts.
        ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
        var storage = new FileEndpointStorage("Bee.Northwind");
        ClientInfo.EndpointStorage = storage;
        ClientInfo.ApiKeyStorage = storage;
        // The shipped key only seeds empty storage on first run; after that the stored value wins,
        // so swapping keys is a settings change rather than a rebuild.
        ClientInfo.ApplyApiKey(AppDefaults.ApiKey);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Builds the Avalonia <see cref="AppBuilder"/>. Kept as a separate method so the
    /// previewer / visual-tree tooling can reuse the same setup.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
