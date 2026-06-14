using System.Collections.ObjectModel;
using Bee.Northwind.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.Northwind.UI.ViewModels;

/// <summary>
/// Terminal step of the flow: the application shell. Owns the left navigation menu
/// (grouped form links) and the collapsible-pane state; the paired <c>FormsView</c>
/// hosts a <c>FormView</c> for the selected link and toggles the pane via
/// <see cref="TogglePaneCommand"/>.
/// </summary>
public partial class FormsViewModel : ViewModelBase
{
    /// <summary>Grouped navigation entries shown in the left menu.</summary>
    public ObservableCollection<NavItem> NavItems { get; } =
    [
        NavItem.Header("Master Data"),
        NavItem.Form("Categories", "Category"),
        NavItem.Form("Suppliers", "Supplier"),
        NavItem.Form("Customers", "Customer"),
        NavItem.Form("Shippers", "Shipper"),
        NavItem.Header("Organization"),
        NavItem.Form("Departments", "Department"),
        NavItem.Form("Employees", "Employee"),
    ];

    /// <summary>
    /// Whether the navigation pane is expanded. Toggled by <see cref="TogglePaneCommand"/>;
    /// the view binds <c>SplitView.IsPaneOpen</c> to it.
    /// </summary>
    [ObservableProperty]
    private bool _isPaneOpen = true;

    /// <summary>Bound to the hamburger button; collapses / expands the navigation pane.</summary>
    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;
}
