using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Bee.Base.Serialization;
using Bee.DefineEditor.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Common shell for singleton-setting tree editors. Holds Title / FilePath /
/// Roots / Issues / IsDirty / StatusText, the Save command (XmlCodec round-trip),
/// the Validate command (delegates to <see cref="PerformValidation"/>), and a
/// generic Delete command driven by <see cref="GetDeleteAction"/>. Subclasses
/// add type-specific Add commands and the tree builder.
/// </summary>
public abstract partial class SingletonDocumentViewModelBase : DocumentViewModelBase
{
    public override string Title { get; }

    public override string DocumentKey => FilePath;

    public string FilePath { get; }

    public ObservableCollection<SettingsTreeNode> Roots { get; } = new();

    public ObservableCollection<ValidationIssue> Issues { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedEditorContext))]
    [NotifyPropertyChangedFor(nameof(SelectedKindCanDelete))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private SettingsTreeNode? _selectedTreeNode;

    partial void OnSelectedTreeNodeChanged(SettingsTreeNode? value)
    {
        // Generator already notifies the [NotifyPropertyChangedFor] targets above;
        // this hook lets subclasses fire their own derived properties (the
        // SelectedKindIsXxx flags powering the tree-view context menu, kind-
        // specific to each settings document).
        OnSelectedTreeNodeRefreshDerivedProperties(value);
    }

    /// <summary>
    /// Override to raise <c>OnPropertyChanged</c> for the subclass's per-kind
    /// derived flags (e.g. <c>SelectedKindIsCategory</c>) so the context-menu
    /// bindings update when the right-clicked tree node changes. Base does
    /// nothing.
    /// </summary>
    protected virtual void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value) { }

    /// <summary>
    /// Whether the currently selected node can be deleted — visibility hint
    /// for the context-menu "刪除" item. Mirrors <c>CanDelete()</c> so the
    /// menu item appears exactly when the command would execute.
    /// </summary>
    public bool SelectedKindCanDelete =>
        SelectedTreeNode is not null && GetDeleteAction(SelectedTreeNode) is not null;

    // IsDirty is inherited from DocumentViewModelBase.

    [ObservableProperty]
    private string _statusText = L("Status_SingletonHint");

    /// <summary>
    /// Right-pane content. Defaults to the selected node's payload; subclasses
    /// override to inject wrapper view-models (e.g. FormSchema's mapping editor).
    /// </summary>
    public virtual object? SelectedEditorContext => SelectedTreeNode?.Payload;

    /// <summary>Underlying mutable object handed to <see cref="XmlCodec.SerializeToFile"/>.</summary>
    protected abstract object RootObject { get; }

    // Forwarders for the base type's uniform file-level command surface — see
    // DocumentViewModelBase.FileSaveCommand. The generated SaveCommand /
    // ValidateCommand below carry the actual logic; this just exposes them
    // under a name the source generator hasn't taken.
    public override IRelayCommand FileSaveCommand => SaveCommand;
    public override IRelayCommand FileValidateCommand => ValidateCommand;

    private string _lastDefaultHint = L("Status_SingletonHint");

    protected SingletonDocumentViewModelBase(string filePath, string titlePrefix, string keyText)
    {
        FilePath = filePath;
        Title = string.IsNullOrEmpty(keyText) ? titlePrefix : $"{titlePrefix} — {keyText}";

        // StatusText is a stored snapshot (not a live binding), so a language
        // switch won't rewrite it automatically. Re-apply the default hint
        // when the culture changes — but only if the user hasn't replaced it
        // with an action message (Save / Validate / Add ...), so we don't
        // wipe their feedback. _lastDefaultHint tracks the most recent hint
        // we wrote so we can recognise it.
        Services.LocalizationService.Current.CultureChanged += (_, _) =>
        {
            var newHint = L("Status_SingletonHint");
            if (StatusText == _lastDefaultHint)
                StatusText = newHint;
            _lastDefaultHint = newHint;
        };
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task Save()
    {
        try
        {
            var issues = PerformValidation();
            if (!await ConfirmSaveAfterValidationAsync(issues, Issues))
            {
                var errs = issues.Count(i => i.Severity == ValidationSeverity.Error);
                StatusText = L("Status_SaveCancelled", errs);
                return;
            }

            foreach (var root in Roots)
                root.RefreshRecursive();
            XmlCodec.SerializeToFile(RootObject, FilePath);
            IsDirty = false;
            StatusText = L("Status_Saved", Path.GetFileName(FilePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText = L("Status_SaveFailed", ex.Message);
        }
    }

    [RelayCommand]
    private void Validate()
    {
        Issues.Clear();
        var found = PerformValidation();
        foreach (var issue in found)
            Issues.Add(issue);

        if (Issues.Count == 0)
        {
            StatusText = L("Status_ValidationPassed");
        }
        else
        {
            var errors = Issues.Count(i => i.Severity == ValidationSeverity.Error);
            var warnings = Issues.Count(i => i.Severity == ValidationSeverity.Warning);
            StatusText = L("Status_ValidationCompleted", Issues.Count, errors, warnings);
        }
    }

    /// <summary>Subclasses produce validation findings here. Default: none.</summary>
    protected virtual IReadOnlyList<ValidationIssue> PerformValidation() =>
        Array.Empty<ValidationIssue>();

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async System.Threading.Tasks.Task Delete()
    {
        var node = SelectedTreeNode;
        if (node is null) return;
        if (GetDeleteAction(node) is not { } action) return;

        if (!await ConfirmDeleteAsync(node.Header))
        {
            StatusText = L("Status_DeleteCancelled");
            return;
        }

        action();
        var parent = node.Parent;
        node.RemoveSelf();
        SelectedTreeNode = parent;
        IsDirty = true;
        StatusText = L("Status_Deleted");
    }

    private bool CanDelete() => SelectedTreeNode is not null && GetDeleteAction(SelectedTreeNode) is not null;

    /// <summary>
    /// Subclasses return a closure that removes <paramref name="node"/> from its
    /// owning Bee.Definition collection, or null when the node is not deletable
    /// (e.g. root nodes). The shell then drops the tree-side node.
    /// </summary>
    protected abstract Action? GetDeleteAction(SettingsTreeNode node);

    /// <summary>
    /// Walks up from <paramref name="node"/> until a node of <paramref name="kind"/> is found.
    /// </summary>
    protected static SettingsTreeNode? FindAncestor(SettingsTreeNode? node, string kind)
    {
        for (var cur = node; cur != null; cur = cur.Parent)
            if (cur.Kind == kind) return cur;
        return null;
    }

    /// <summary>Picks a candidate key not already present in <paramref name="existing"/>.</summary>
    protected static string UniqueKey(IEnumerable<string> existing, string baseName)
    {
        var set = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        if (!set.Contains(baseName)) return baseName;
        for (int i = 2; ; i++)
        {
            var candidate = $"{baseName}{i}";
            if (!set.Contains(candidate)) return candidate;
        }
    }
}
