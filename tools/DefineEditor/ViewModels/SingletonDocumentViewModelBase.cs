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
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private SettingsTreeNode? _selectedTreeNode;

    // IsDirty is inherited from DocumentViewModelBase.

    [ObservableProperty]
    private string _statusText = "（屬性編輯於離開欄位時寫入記憶體；按「儲存」會刷新樹節點顯示並寫回 XML）";

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

    protected SingletonDocumentViewModelBase(string filePath, string titlePrefix, string keyText)
    {
        FilePath = filePath;
        Title = string.IsNullOrEmpty(keyText) ? titlePrefix : $"{titlePrefix} — {keyText}";
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
                StatusText = $"已取消儲存（{errs} 個 error 未處理）。";
                return;
            }

            foreach (var root in Roots)
                root.RefreshRecursive();
            XmlCodec.SerializeToFile(RootObject, FilePath);
            IsDirty = false;
            StatusText = $"已儲存：{Path.GetFileName(FilePath)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText = $"儲存失敗：{ex.Message}";
        }
    }

    [RelayCommand]
    private void Validate()
    {
        Issues.Clear();
        var found = PerformValidation();
        foreach (var issue in found)
            Issues.Add(issue);

        StatusText = Issues.Count == 0
            ? "驗證通過：未發現任何問題。"
            : $"驗證完成：{Issues.Count} 個問題（{Issues.Count(i => i.Severity == ValidationSeverity.Error)} Error / {Issues.Count(i => i.Severity == ValidationSeverity.Warning)} Warning）。";
    }

    /// <summary>Subclasses produce validation findings here. Default: none.</summary>
    protected virtual IReadOnlyList<ValidationIssue> PerformValidation() =>
        Array.Empty<ValidationIssue>();

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        var node = SelectedTreeNode;
        if (node is null) return;
        if (GetDeleteAction(node) is not { } action) return;

        action();
        var parent = node.Parent;
        node.RemoveSelf();
        SelectedTreeNode = parent;
        IsDirty = true;
        StatusText = "已刪除節點（尚未存檔）";
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
