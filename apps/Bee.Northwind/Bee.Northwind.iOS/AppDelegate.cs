using Avalonia;
using Avalonia.iOS;
using Bee.Api.Client;
using Bee.Northwind.UI;
using Bee.UI.Avalonia.Storage;
using Bee.UI.Core;
using Foundation;

namespace Bee.Northwind.iOS;

/// <summary>
/// iOS application delegate. Wires the Bee client-side singletons before any Avalonia control
/// runs — the same client contract as the desktop / browser heads — then lets
/// <see cref="AvaloniaAppDelegate{TApp}"/> host the shared <see cref="App"/> from
/// <c>Bee.Northwind.UI</c> as the single view (iOS has no native window).
/// </summary>
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix — "Delegate" is the iOS convention.
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711
{
    /// <inheritdoc/>
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Configure the Bee client singletons before the Avalonia app initialises. iOS sandboxes
        // the app, but FileEndpointStorage writes under the per-user local application data folder
        // (SpecialFolder.LocalApplicationData maps to the app's writable Library directory on iOS),
        // so the desktop file-backed storage applies unchanged. Verified in plan stage 3.
        ApiClientInfo.ApiKey = AppDefaults.ApiKey;
        ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
        ClientInfo.EndpointStorage = new FileEndpointStorage("Bee.Northwind");

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
