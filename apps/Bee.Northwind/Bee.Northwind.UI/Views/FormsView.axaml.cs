using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Bee.Northwind.UI.Models;
using Bee.UI.Avalonia.Controls;

namespace Bee.Northwind.UI.Views;

/// <summary>
/// Application shell. Hosts the collapsible navigation menu and swaps the content host to a
/// freshly created <see cref="FormView"/> whenever a form link is selected — a new control
/// per selection so each one runs its attach-time initialisation (FormView resolves its
/// schema / connector lazily in <c>OnAttachedToVisualTree</c>, so reusing one instance and
/// only changing <c>ProgId</c> would not re-initialise it).
/// </summary>
/// <remarks>
/// The code-behind relies on the source-generated <c>InitializeComponent</c> so the
/// <c>x:Name</c> controls (<c>NavList</c> / <c>FormHost</c>) are wired into fields. A
/// hand-written <c>InitializeComponent</c> calling <c>AvaloniaXamlLoader.Load</c> would
/// load the XAML but leave those fields null.
/// </remarks>
public partial class FormsView : UserControl
{
    private bool _initialSelectionDone;

    /// <summary>
    /// Initializes a new instance of <see cref="FormsView"/>.
    /// </summary>
    public FormsView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Select the first form link once the bound DataContext has populated the menu.
        // Doing this in the constructor is too early: the ViewLocator assigns DataContext
        // after construction, so NavList.Items would still be empty.
        if (_initialSelectionDone) return;
        _initialSelectionDone = true;
        SelectFirstForm();
    }

    private void SelectFirstForm()
    {
        foreach (var item in NavList.Items)
        {
            if (item is NavItem { IsHeader: false })
            {
                NavList.SelectedItem = item;
                return;
            }
        }
    }

    private void OnNavSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is NavItem { IsHeader: false, ProgId.Length: > 0 } item)
        {
            FormHost.Content = new FormView { ProgId = item.ProgId };
        }
    }

    private void OnThemeToggled(object? sender, RoutedEventArgs e)
    {
        if (global::Avalonia.Application.Current is { } app)
        {
            app.RequestedThemeVariant = ThemeToggle.IsChecked == true
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }
    }
}
