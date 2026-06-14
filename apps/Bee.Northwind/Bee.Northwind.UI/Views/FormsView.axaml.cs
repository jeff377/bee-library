using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
using Bee.Northwind.UI.Controls;
using Bee.Northwind.UI.Models;

namespace Bee.Northwind.UI.Views;

/// <summary>
/// Application shell. The left menu opens each form as a document tab hosting a
/// <see cref="FormWorkspace"/> (the ERP list ↔ record flow lives inside the workspace);
/// one tab per form, de-duplicated by program id.
/// </summary>
/// <remarks>
/// Opening is driven from the menu's <c>Tapped</c> event rather than <c>SelectionChanged</c>
/// so tapping an already-selected entry still re-opens its tab. The code-behind relies on the
/// source-generated <c>InitializeComponent</c> so the <c>x:Name</c> controls are wired into
/// fields.
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

        // Open the first form once the bound DataContext has populated the menu. Doing this
        // in the constructor is too early: the ViewLocator assigns DataContext afterwards.
        if (_initialSelectionDone) return;
        _initialSelectionDone = true;

        var first = NavList.Items.OfType<NavItem>().FirstOrDefault(n => !n.IsHeader);
        if (first is not null)
        {
            NavList.SelectedItem = first;
            OpenForm(first);
        }
    }

    private void OnNavTapped(object? sender, TappedEventArgs e)
    {
        // By the time Tapped fires the click has already updated the selection, so the
        // tapped entry is SelectedItem — including when it was already selected.
        if (NavList.SelectedItem is NavItem { IsHeader: false, ProgId.Length: > 0 } item)
        {
            OpenForm(item);
        }
    }

    private void OpenForm(NavItem item)
    {
        foreach (var existing in Tabs.Items.OfType<TabItem>())
        {
            if ((string?)existing.Tag == item.ProgId)
            {
                Tabs.SelectedItem = existing;
                return;
            }
        }

        var tab = new TabItem
        {
            Tag = item.ProgId,
            Header = BuildTabHeader(item.Title, item.ProgId),
            Content = new FormWorkspace(item.ProgId),
        };
        Tabs.Items.Add(tab);
        Tabs.SelectedItem = tab;
    }

    private Control BuildTabHeader(string title, string tag)
    {
        var text = new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center };
        var close = new Button
        {
            Content = "✕",
            Tag = tag,
            Classes = { "tabclose" },
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
        };
        close.Click += OnCloseTabClick;

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(text);
        panel.Children.Add(close);
        return panel;
    }

    private void OnCloseTabClick(object? sender, RoutedEventArgs e)
    {
        // Stop the click from also selecting the tab being closed.
        e.Handled = true;
        if (sender is Button { Tag: string tag })
        {
            CloseTab(tag);
        }
    }

    private void CloseTab(string tag)
    {
        var tab = Tabs.Items.OfType<TabItem>().FirstOrDefault(t => (string?)t.Tag == tag);
        if (tab is null) return;

        var index = Tabs.Items.IndexOf(tab);
        Tabs.Items.Remove(tab);

        if (Tabs.Items.Count > 0)
        {
            Tabs.SelectedIndex = global::System.Math.Min(index, Tabs.Items.Count - 1);
        }

        SyncNavToActiveTab();
    }

    /// <summary>Highlights the menu entry matching the currently active tab.</summary>
    private void SyncNavToActiveTab()
    {
        var activeProgId = (Tabs.SelectedItem as TabItem)?.Tag as string;
        NavList.SelectedItem = activeProgId is null
            ? null
            : NavList.Items.OfType<NavItem>().FirstOrDefault(n => n.ProgId == activeProgId);
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
