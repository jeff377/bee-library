using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Bee.DefineEditor.Models;
using Bee.DefineEditor.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Base type for the right-pane document editors. Every concrete editor (FormSchema,
/// singleton settings, etc.) inherits this so the shell can host them through the
/// <see cref="Avalonia.Controls.TabControl"/> in MainWindow with views resolved via
/// <see cref="ViewLocator"/>.
/// </summary>
public abstract partial class DocumentViewModelBase : ViewModelBase
{
    /// <summary>Header shown in the tab and inside the document.</summary>
    public abstract string Title { get; }

    /// <summary>
    /// Stable identity used by the shell to detect "already open" — typically the
    /// canonical file path. Two documents with the same key are treated as the
    /// same tab; selecting the source node activates it rather than re-loading.
    /// </summary>
    public abstract string DocumentKey { get; }

    /// <summary>
    /// Tracks unsaved edits so the tab can show a modified indicator. Concrete
    /// editors set this when their underlying schema is mutated and clear it on
    /// successful save.
    /// </summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>
    /// Resource key of the geometry shown left of the title in the tab. Looked
    /// up against the application-level resource dictionary via the icon-key
    /// converter (see <see cref="Converters.IconKeyToGeometryConverter"/>).
    /// </summary>
    public virtual string TabIcon => "DefUnknown";

    /// <summary>
    /// Replays <paramref name="issues"/> into <paramref name="issuesPanel"/> and,
    /// when any error-severity finding is present, prompts the user via a modal
    /// dialog whether to save anyway. Returns <c>true</c> when the caller should
    /// proceed with the file write; <c>false</c> when the user cancelled.
    /// </summary>
    /// <remarks>
    /// In headless contexts (smoke tests; no <see cref="IClassicDesktopStyleApplicationLifetime"/>)
    /// the dialog is skipped and the call resolves to <c>true</c> so saves still
    /// proceed without UI interaction.
    /// </remarks>
    protected static async Task<bool> ConfirmSaveAfterValidationAsync(
        IReadOnlyList<ValidationIssue> issues,
        ObservableCollection<ValidationIssue> issuesPanel)
    {
        issuesPanel.Clear();
        foreach (var issue in issues)
            issuesPanel.Add(issue);

        var errors = issues.Count(i => i.Severity == ValidationSeverity.Error);
        if (errors == 0) return true;

        var owner = GetOwnerWindow();
        if (owner is null) return true; // smoke / headless

        var warns = issues.Count(i => i.Severity == ValidationSeverity.Warning);
        var message = warns > 0
            ? $"驗證發現 {errors} 個 error、{warns} 個 warning。\n仍要繼續存檔嗎？"
            : $"驗證發現 {errors} 個 error。\n仍要繼續存檔嗎？";
        return await ConfirmationDialog.ShowAsync(owner, "存檔前驗證未通過", message);
    }

    private static Window? GetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            return lifetime.MainWindow;
        return null;
    }
}
