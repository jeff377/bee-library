using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Bee.Api.Client;
using Bee.Northwind.UI;
using Bee.UI.Avalonia.Storage;
using Bee.UI.Core;

namespace Bee.Northwind.Android;

/// <summary>
/// Android application object — the analog of the iOS <c>AppDelegate</c>. Android instantiates
/// it once at process start, before any activity, and <see cref="AvaloniaAndroidApplication{TApp}"/>
/// builds the single-view Avalonia lifetime here (the <see cref="MainActivity"/> only hosts the
/// resulting view). This is therefore the place to wire the Bee client-side singletons — the same
/// client contract as the desktop / browser / iOS heads — before any Avalonia control runs.
/// </summary>
[Application]
public class Application : AvaloniaAndroidApplication<App>
{
    /// <summary>
    /// Required JNI bridge constructor. The Android runtime calls this when it materialises the
    /// managed peer for the native application instance.
    /// </summary>
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    /// <inheritdoc/>
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Configure the Bee client singletons before the Avalonia app initialises. The Android
        // sandbox grants the app a writable per-user data directory, and FileEndpointStorage
        // writes under SpecialFolder.LocalApplicationData (which maps there on Android), so the
        // desktop file-backed storage applies unchanged. Verified in plan stage 3.
        ApiClientInfo.ApiKey = AppDefaults.ApiKey;
        ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
        ClientInfo.EndpointStorage = new FileEndpointStorage("Bee.Northwind");

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
