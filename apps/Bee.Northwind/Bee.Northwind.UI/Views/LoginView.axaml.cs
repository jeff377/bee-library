using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Bee.Northwind.UI.Views;

/// <summary>
/// Code-behind for <see cref="LoginView"/>. Only loads the XAML — all behaviour lives on
/// <see cref="ViewModels.LoginViewModel"/>.
/// </summary>
public partial class LoginView : UserControl
{
    /// <summary>Loads the XAML.</summary>
    public LoginView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
