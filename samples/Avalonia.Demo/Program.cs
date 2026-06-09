using Avalonia;
using Bee.Api.Client;
using Bee.UI.Avalonia.Storage;
using Bee.UI.Core;

namespace Avalonia.Demo
{
    /// <summary>
    /// Process entry point. Wires the Bee client-side singletons
    /// (<see cref="ApiClientInfo"/> + <see cref="ClientInfo.EndpointStorage"/>)
    /// before any Avalonia control instantiates and then hands control to the
    /// classic-desktop Avalonia lifetime.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Default JSON-RPC endpoint pre-filled in the connection screen.
        /// Matches the <c>QuickStart.Server</c> sample's default listen URL.
        /// </summary>
        public const string DefaultEndpoint = "http://localhost:5050/api";

        /// <summary>
        /// Application entry point. <c>STAThread</c> is required by Windows
        /// for Avalonia to drive native dialogs / drag-drop / clipboard.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            // Configure the Bee client singletons before any control or VM runs.
            // EndpointStorage must point at a writable per-user folder; the lib's
            // FileEndpointStorage handles that for unpackaged Avalonia hosts.
            ApiClientInfo.ApiKey = "avalonia-demo";
            ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
            ClientInfo.EndpointStorage = new FileEndpointStorage("Bee.Avalonia.Demo");

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        /// <summary>
        /// Builds the Avalonia <see cref="AppBuilder"/>. Kept as a separate method
        /// so previewer / visual-tree tooling can reuse the same setup.
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
