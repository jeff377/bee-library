using Maui.Demo.Pages;

namespace Maui.Demo;

/// <summary>
/// Shell host. Registers the routes that the demo navigates between
/// (<see cref="ConnectionPage"/> → <see cref="LoginPage"/> → <see cref="EmployeePage"/>).
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>Initializes XAML resources and registers navigation routes.</summary>
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(EmployeePage), typeof(EmployeePage));
    }
}
