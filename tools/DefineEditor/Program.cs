using Avalonia;

namespace Bee.DefineEditor;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--smoke")
            return Smoke.Run(args[1]);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new MacOSPlatformOptions
            {
                // 關掉預設的 macOS 應用程式選單（About Avalonia / Quit 等），
                // 改由 App.ConfigureNativeAppMenu 自己組裝有 About 我們版本的選單。
                DisableDefaultApplicationMenuItems = true,
            })
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
