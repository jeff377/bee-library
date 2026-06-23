using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Bee.Northwind.UI.ViewModels;
using Bee.Northwind.UI.Views;

namespace Bee.Northwind.UI;

/// <summary>
/// Application root shared by every platform head. <see cref="OnFrameworkInitializationCompleted"/>
/// hosts a <see cref="MainWindowViewModel"/>-seeded <see cref="MainView"/>: the desktop head wraps
/// it in a <see cref="MainWindow"/>, the browser (WASM) head shows it as the single view.
/// Navigation between Connection → Login → Forms is then driven by the VM.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // Browser (WASM) head: the sandbox has no native window, so host the shared
            // MainView directly as the application's single view.
            singleView.MainView = new MainView
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
