using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Bee.Northwind.UI.ViewModels;
using Bee.Northwind.UI.Views;

namespace Bee.Northwind.UI;

/// <summary>
/// Application root shared by every platform head. <see cref="OnFrameworkInitializationCompleted"/>
/// creates the <see cref="MainWindow"/> seeded with a <see cref="MainWindowViewModel"/>;
/// navigation between Connection → Login → Forms is then driven by the VM.
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

        base.OnFrameworkInitializationCompleted();
    }
}
