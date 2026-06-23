using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Bee.Northwind.UI.Views;

/// <summary>
/// Platform-neutral root content hosting the navigation <see cref="ContentControl"/> bound to
/// <c>CurrentView</c> on <see cref="ViewModels.MainWindowViewModel"/>. The desktop head wraps it
/// in <see cref="MainWindow"/>; the browser head hosts it directly as the single view.
/// </summary>
public partial class MainView : UserControl
{
    /// <summary>Loads the XAML.</summary>
    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
