using System.Collections.ObjectModel;
using Bee.Definition;
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
/// The menu is built from <see cref="ProgramSettings"/> fetched from the server, so the
/// navigation is pure definition — adding a form to the menu is a ProgramSettings.xml entry,
/// not a code change. The fetch goes directly through the async connector
/// (<see cref="ClientInfo.SystemApiConnector"/>) rather than the synchronous
/// <see cref="ClientInfo.DefineAccess"/>, whose <c>SyncExecutor</c> bridge blocks the single
/// browser-wasm thread ("Cannot wait on monitors"). It runs once, right after login, when the
/// session token is already set.
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
    /// Builds the menu from the server's <see cref="ProgramSettings"/>: each category becomes a
    /// header row, each program item a form link.
    /// </summary>
    private async Task LoadNavItemsAsync()
    {
        ProgramSettings settings;
        try
        {
            settings = await ClientInfo.SystemApiConnector
                .GetDefineAsync<ProgramSettings>(DefineType.ProgramSettings)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // A failed menu load must not crash the shell as an unobserved task exception;
            // surface it as a disabled header so the empty menu is not silent.
            NavItems.Add(NavItem.Header($"(menu load failed: {ex.Message})"));
            return;
        }

        if (settings?.Categories is null) { return; }

        foreach (var category in settings.Categories)
        {
            NavItems.Add(NavItem.Header(category.DisplayName));
            if (category.Items is null) { continue; }
            foreach (var item in category.Items)
                NavItems.Add(NavItem.Form(item.DisplayName, item.ProgId));
        }
    }

    /// <summary>Bound to the hamburger button; collapses / expands the navigation pane.</summary>
    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;
}
