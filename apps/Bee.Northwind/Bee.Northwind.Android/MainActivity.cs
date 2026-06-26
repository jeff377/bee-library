using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace Bee.Northwind.Android;

/// <summary>
/// The single launcher activity. It hosts the Avalonia view produced by <see cref="Application"/>'s
/// single-view lifetime; all client wiring and app-builder configuration live there, so this type
/// only declares the Android launch metadata. <see cref="ConfigChanges.Orientation"/> /
/// <see cref="ConfigChanges.ScreenSize"/> let the activity handle rotation without being recreated,
/// so Avalonia reflows the layout in place (responsive layout is exercised in plan stage 4).
/// </summary>
[Activity(
    Label = "Bee.Northwind",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
