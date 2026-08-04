using System.Collections.ObjectModel;
using Bee.Definition.Settings;
using Bee.Northwind.UI.Models;
using Bee.UI.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.Northwind.UI.ViewModels;

/// <summary>
/// Terminal step of the flow: the application shell. Owns the left navigation menu
/// (grouped form links) and the collapsible-pane state; the paired <c>FormsView</c>
/// hosts a <c>FormView</c> for the selected link and toggles the pane via
/// <see cref="TogglePaneCommand"/>.
/// </summary>
/// <remarks>
/// The menu is built from <see cref="MenuSettings"/> fetched from the server, so the navigation
/// is pure definition — adding a form to the menu is a MenuSettings.xml entry, not a code change.
/// The fetch goes through <see cref="ClientInfo.DefineAccess"/>, the async typed definition cache;
/// it is safe on the single browser-wasm thread (no sync-over-async bridge) and serves later reads
/// from cache. It runs once, right after login, when the session token is already set.
/// <para>
/// The definition allows folders to nest arbitrarily, while this shell's menu is a flat list of
/// headers and links, so nested folders are flattened into successive header rows. A shell with a
/// tree control would bind the node hierarchy directly instead.
/// </para>
/// </remarks>
public partial class FormsViewModel : ViewModelBase
{
    /// <summary>Grouped navigation entries shown in the left menu.</summary>
    public ObservableCollection<NavItem> NavItems { get; } = [];

    /// <summary>
    /// Whether the navigation pane is expanded. Toggled by <see cref="TogglePaneCommand"/>;
    /// the view binds <c>SplitView.IsPaneOpen</c> to it.
    /// </summary>
    [ObservableProperty]
    private bool _isPaneOpen = true;

    /// <summary>
    /// Initialises the shell and kicks off the asynchronous navigation-menu load.
    /// </summary>
    public FormsViewModel()
    {
        // Fire-and-forget: the menu populates the bound ObservableCollection when the fetch
        // completes. ConfigureAwait(true) keeps the continuation on the UI thread.
        _ = LoadNavItemsAsync();
    }

    /// <summary>
    /// Builds the menu from the server's <see cref="MenuSettings"/>: each folder becomes a header
    /// row, each entry a form link.
    /// </summary>
    private async Task LoadNavItemsAsync()
    {
        MenuSettings settings;
        try
        {
            settings = await ClientInfo.DefineAccess
                .GetMenuSettingsAsync()
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // A failed menu load must not crash the shell as an unobserved task exception;
            // surface it as a disabled header so the empty menu is not silent.
            NavItems.Add(NavItem.Header($"(menu load failed: {ex.Message})"));
            return;
        }

        AddNodes(settings?.Items);
    }

    /// <summary>
    /// Appends one level of menu nodes, recursing into folders.
    /// </summary>
    /// <param name="nodes">The nodes to append; <c>null</c> appends nothing.</param>
    private void AddNodes(MenuNodeCollection? nodes)
    {
        if (nodes is null) { return; }

        // GetDisplayNodes applies Order and the Visible switch, so every UI head orders the menu
        // the same way.
        foreach (var node in nodes.GetDisplayNodes())
        {
            switch (node)
            {
                case MenuFolder folder:
                    NavItems.Add(NavItem.Header(folder.Caption));
                    AddNodes(folder.Items);
                    break;
                case MenuEntry entry:
                    NavItems.Add(NavItem.Form(entry.Caption, entry.ProgId));
                    break;
            }
        }
    }

    /// <summary>Bound to the hamburger button; collapses / expands the navigation pane.</summary>
    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;
}
