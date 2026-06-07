using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Bee.DefineEditor.ViewModels;
using Bee.DefineEditor.Views;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor;

public partial class App : Application
{
    public IRelayCommand AboutCommand { get; }
    public IRelayCommand HideCommand { get; }
    public IRelayCommand QuitCommand { get; }

    public App()
    {
        AboutCommand = new RelayCommand(ShowAbout);
        HideCommand = new RelayCommand(HideAllWindows);
        QuitCommand = new RelayCommand(Quit);
    }

    public override void Initialize()
    {
        // macOS 主選單列的應用程式名（NSApplication.AppName）。
        Name = "Bee.DefineEditor";
        AvaloniaXamlLoader.Load(this);
        // NativeMenu 必須在 framework 初始化前掛好；否則
        // AvaloniaNativeMenuExporter.CreateDefaultAppMenu 已經把
        // "About Avalonia" 加進預設選單，後面才設就替換不掉。
        ConfigureNativeAppMenu();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Builds the macOS application menu manually. <c>Program.cs</c> sets
    /// <see cref="MacOSPlatformOptions.DisableDefaultApplicationMenuItems"/>=true,
    /// so Avalonia no longer adds "About Avalonia" / "Quit" itself — we provide
    /// the full app menu (About / Hide / Quit) with our own About wired to
    /// <see cref="AboutDialog"/>.
    /// </summary>
    private void ConfigureNativeAppMenu()
    {
        // 直接把項目放在 top-level：Avalonia 把這些併入 macOS app menu
        // （而不是新建一個 "Bee.DefineEditor" 子選單）。
        var menu = new NativeMenu();
        menu.Add(new NativeMenuItem("About Bee.DefineEditor")
        {
            Command = AboutCommand,
        });
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(new NativeMenuItem("Hide Bee.DefineEditor")
        {
            Command = HideCommand,
            Gesture = new KeyGesture(Key.H, KeyModifiers.Meta),
        });
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(new NativeMenuItem("Quit Bee.DefineEditor")
        {
            Command = QuitCommand,
            Gesture = new KeyGesture(Key.Q, KeyModifiers.Meta),
        });
        NativeMenu.SetMenu(this, menu);
    }

    private static void ShowAbout()
    {
        var dialog = new AboutDialog();
        var owner = (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is not null)
            dialog.ShowDialog(owner);
        else
            dialog.Show();
    }

    private static void HideAllWindows()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            foreach (var w in lifetime.Windows)
                w.WindowState = WindowState.Minimized;
    }

    private static void Quit()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Shutdown();
    }

    /// <summary>
    /// Toggles the application-wide theme between Dark and Light. Called from
    /// the toolbar's theme button in MainWindow.
    /// </summary>
    public static void ToggleTheme()
    {
        if (Current is null) return;
        Current.RequestedThemeVariant =
            Current.ActualThemeVariant == ThemeVariant.Light
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
    }
}
