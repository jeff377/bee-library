using Foundation;

namespace Maui.Demo;

/// <summary>
/// Mac Catalyst entry point. Forwards control to <see cref="MauiProgram.CreateMauiApp"/>.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
