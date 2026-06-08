using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bee.Base.Serialization;
using Bee.Definition.Settings;
using Bee.DefineEditor.Models;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Editor for <see cref="DbCategorySettings"/>. Two-level tree:
/// DbCategorySettings → DbCategory[] → TableItem[]. Validation: duplicate /
/// empty category Ids, duplicate / empty table names within a category.
/// </summary>
public sealed partial class DbCategorySettingsDocumentViewModel : SingletonDocumentViewModelBase
{
    public const string KindRoot = "DbCategorySettings";
    public const string KindCategory = "DbCategory";
    public const string KindTable = "TableItem";

    public DbCategorySettings Root { get; }

    protected override object RootObject => Root;

    public override string TabIcon => "DefDbCategorySettings";

    // Visibility flags for the tree-view context menu. Each MenuItem binds
    // IsVisible to the flag matching the kind it applies to.
    public bool SelectedKindIsRoot => SelectedTreeNode?.Kind == KindRoot;
    public bool SelectedKindIsCategory => SelectedTreeNode?.Kind == KindCategory;

    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsRoot));
        OnPropertyChanged(nameof(SelectedKindIsCategory));
    }

    private DbCategorySettingsDocumentViewModel(string filePath, DbCategorySettings root)
        : base(filePath, "DbCategorySettings", keyText: string.Empty)
    {
        Root = root;
        Roots.Add(BuildRootNode(root));
        SelectedTreeNode = Roots[0];
    }

    public static DbCategorySettingsDocumentViewModel Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("DbCategorySettings file not found.", filePath);
        var root = XmlCodec.DeserializeFromFile<DbCategorySettings>(filePath)
            ?? throw new InvalidOperationException($"DbCategorySettings deserialized to null: {filePath}");
        return new DbCategorySettingsDocumentViewModel(filePath, root);
    }

    private static SettingsTreeNode BuildRootNode(DbCategorySettings root)
    {
        var node = MakeNode("DefDbCategorySettings", KindRoot, root, RefreshRoot, isExpanded: true);
        if (root.Categories is { } categories)
            foreach (var category in categories)
                node.AddChild(BuildCategoryNode(category));
        return node;
    }

    private static SettingsTreeNode BuildCategoryNode(DbCategory category)
    {
        var node = MakeNode("DefCategory", KindCategory, category, RefreshCategory, isExpanded: false);
        if (category.Tables is { } tables)
            foreach (var table in tables)
                node.AddChild(BuildTableNode(table));
        return node;
    }

    private static SettingsTreeNode BuildTableNode(TableItem table) =>
        MakeNode("IconTable", KindTable, table, RefreshTable, isExpanded: false);

    private static void RefreshRoot(SettingsTreeNode node)
    {
        var root = (DbCategorySettings)node.Payload!;
        node.Header = "DbCategorySettings";
        node.Detail = $"{root.Categories?.Count ?? 0} DbCategory item(s)";
    }

    private static void RefreshCategory(SettingsTreeNode node)
    {
        var c = (DbCategory)node.Payload!;
        node.Header = $"{c.Id}  —  {c.DisplayName}";
        node.Detail = string.Join(Environment.NewLine,
            $"Id：{c.Id}",
            $"DisplayName：{c.DisplayName}",
            $"Tables：{c.Tables?.Count ?? 0}");
    }

    private static void RefreshTable(SettingsTreeNode node)
    {
        var t = (TableItem)node.Payload!;
        node.Header = $"{t.TableName}  —  {t.DisplayName}";
        node.Detail = string.Join(Environment.NewLine,
            $"TableName：{t.TableName}",
            $"DisplayName：{t.DisplayName}");
    }

    private static SettingsTreeNode MakeNode(
        string icon, string kind, object payload,
        Action<SettingsTreeNode> refresher, bool isExpanded)
    {
        var node = new SettingsTreeNode
        {
            Icon = icon,
            Kind = kind,
            Payload = payload,
            IsExpanded = isExpanded,
            Refresher = refresher,
        };
        node.RefreshDisplay();
        return node;
    }

    [RelayCommand(CanExecute = nameof(CanAddCategory))]
    private void AddCategory()
    {
        if (SelectedTreeNode is not { Kind: KindRoot, Payload: DbCategorySettings root } rootNode)
            return;
        var id = UniqueKey(root.Categories!.Select(c => c.Id), "new_category");
        var category = new DbCategory { Id = id, DisplayName = "New category" };
        root.Categories!.Add(category);
        var node = BuildCategoryNode(category);
        rootNode.AddChild(node);
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "DbCategory", id);
    }

    private bool CanAddCategory() => SelectedTreeNode?.Kind == KindRoot;

    [RelayCommand(CanExecute = nameof(CanAddTable))]
    private void AddTable()
    {
        var categoryNode = FindAncestor(SelectedTreeNode, KindCategory);
        if (categoryNode?.Payload is not DbCategory category) return;
        var name = UniqueKey(category.Tables!.Select(t => t.TableName), "new_table");
        var table = new TableItem { TableName = name, DisplayName = "New table" };
        category.Tables!.Add(table);
        var node = BuildTableNode(table);
        categoryNode.AddChild(node);
        categoryNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "TableItem", name);
    }

    private bool CanAddTable() => FindAncestor(SelectedTreeNode, KindCategory) is not null;

    protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
    {
        KindCategory when node.Payload is DbCategory c && node.Parent?.Payload is DbCategorySettings p
            => () => p.Categories!.Remove(c),
        KindTable when node.Payload is TableItem t && node.Parent?.Payload is DbCategory pc
            => () => pc.Tables!.Remove(t),
        _ => null,
    };

    protected override IReadOnlyList<ValidationIssue> PerformValidation()
    {
        var issues = new List<ValidationIssue>();
        var categories = Root.Categories;
        if (categories is null || categories.Count == 0)
        {
            issues.Add(new(ValidationSeverity.Warning, "DbCategorySettings", "No DbCategory has been defined."));
            return issues;
        }
        var seenCategoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in categories)
        {
            var catPath = !string.IsNullOrWhiteSpace(category.Id) ? category.Id : "(unnamed)";
            if (string.IsNullOrWhiteSpace(category.Id))
                issues.Add(new(ValidationSeverity.Error, catPath, "DbCategory.Id cannot be empty."));
            else if (!seenCategoryIds.Add(category.Id))
                issues.Add(new(ValidationSeverity.Error, catPath,
                    $"DbCategory.Id '{category.Id}' is a duplicate."));

            var seenTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in category.Tables ?? Enumerable.Empty<TableItem>())
            {
                var path = $"{catPath}.{(string.IsNullOrEmpty(table.TableName) ? "(unnamed)" : table.TableName)}";
                if (string.IsNullOrWhiteSpace(table.TableName))
                    issues.Add(new(ValidationSeverity.Error, path, "TableItem.TableName cannot be empty."));
                else if (!seenTableNames.Add(table.TableName))
                    issues.Add(new(ValidationSeverity.Error, path,
                        $"TableItem.TableName '{table.TableName}' is a duplicate within '{catPath}'."));
            }
        }
        return issues;
    }
}
