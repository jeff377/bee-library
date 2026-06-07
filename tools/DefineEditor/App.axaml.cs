using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Bee.DefineEditor.ViewModels;
using Bee.DefineEditor.Views;

namespace Bee.DefineEditor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
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
