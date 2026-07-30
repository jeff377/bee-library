using Avalonia;
using Avalonia.Browser;
using Bee.Api.Client;
using Bee.Northwind.Browser.Storage;
using Bee.Northwind.UI;
using Bee.UI.Core;

internal sealed partial class Program
{
    /// <summary>
    /// Browser (WASM) head entry point. Wires the Bee client-side singletons before any
    /// Avalonia control runs, then starts the Avalonia browser app against the
    /// <c>&lt;div id="out"&gt;</c> mount point in <c>wwwroot/index.html</c>.
    /// </summary>
    private static Task Main(string[] args)
    {
        // Same client contract as the desktop head (Remote connector to the JSON-RPC
        // backend), but the browser sandbox cannot write files, so endpoint persistence
        // goes through localStorage instead of FileEndpointStorage.
        ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
        var storage = new BrowserLocalStorageEndpointStorage("Bee.Northwind");
        ClientInfo.EndpointStorage = storage;
        ClientInfo.ApiKeyStorage = storage;
        // The shipped key only seeds empty storage on first run; after that the stored value wins,
        // so swapping keys is a settings change rather than a rebuild.
        ClientInfo.ApplyApiKey(AppDefaults.ApiKey);

        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
