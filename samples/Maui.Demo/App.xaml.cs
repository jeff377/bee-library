namespace Maui.Demo;

/// <summary>
/// MAUI application entry. Hosts <see cref="AppShell"/> as the root window's
/// content so the rest of the app can navigate via Shell routes.
/// </summary>
public partial class App : Application
{
    /// <summary>Initializes XAML resources.</summary>
    public App()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell()) { Title = "Bee MAUI Demo" };
    }
}
