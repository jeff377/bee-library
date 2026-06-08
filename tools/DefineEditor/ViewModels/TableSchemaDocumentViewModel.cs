using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Database;
using Bee.Definition.Sorting;
using Bee.DefineEditor.Models;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Editor for <see cref="TableSchema"/>. Tree: TableSchema → Fields group →
/// DbField[]; TableSchema → Indexes group → DbTableIndex[] → IndexField[].
/// </summary>
public sealed partial class TableSchemaDocumentViewModel : SingletonDocumentViewModelBase
{
    public const string KindRoot = "TableSchema";
    public const string KindFieldsGroup = "FieldsGroup";
    public const string KindField = "DbField";
    public const string KindIndexesGroup = "IndexesGroup";
    public const string KindIndex = "DbTableIndex";
    public const string KindIndexField = "IndexField";

    public TableSchema Root { get; }
    protected override object RootObject => Root;

    public override string TabIcon => "DefTableSchema";

    public bool SelectedKindIsFieldsGroup => SelectedTreeNode?.Kind == KindFieldsGroup;
    public bool SelectedKindIsIndexesGroup => SelectedTreeNode?.Kind == KindIndexesGroup;
    public bool SelectedKindIsIndex => SelectedTreeNode?.Kind == KindIndex;

    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsFieldsGroup));
        OnPropertyChanged(nameof(SelectedKindIsIndexesGroup));
        OnPropertyChanged(nameof(SelectedKindIsIndex));
    }

    private TableSchemaDocumentViewModel(string filePath, TableSchema root)
        : base(filePath, "TableSchema", keyText: root.TableName)
    {
        Root = root;
        Roots.Add(BuildRootNode(root));
        SelectedTreeNode = Roots[0];
    }

    public static TableSchemaDocumentViewModel Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("TableSchema file not found.", filePath);
        var root = XmlCodec.DeserializeFromFile<TableSchema>(filePath)
            ?? throw new InvalidOperationException($"TableSchema deserialized to null: {filePath}");
        return new TableSchemaDocumentViewModel(filePath, root);
    }

    private static SettingsTreeNode BuildRootNode(TableSchema root)
    {
        var rootNode = MakeNode("DefTableSchema", KindRoot, root, RefreshRoot, isExpanded: true);

        var fieldsGroup = MakeNode("IconColumn", KindFieldsGroup, root, RefreshFieldsGroup, isExpanded: true);
        if (root.Fields is { } fields)
            foreach (var f in fields)
                fieldsGroup.AddChild(BuildFieldNode(f));
        rootNode.AddChild(fieldsGroup);

        var indexesGroup = MakeNode("IconKey", KindIndexesGroup, root, RefreshIndexesGroup, isExpanded: true);
        if (root.Indexes is { } indexes)
            foreach (var ix in indexes)
                indexesGroup.AddChild(BuildIndexNode(ix));
        rootNode.AddChild(indexesGroup);

        return rootNode;
    }

    private static SettingsTreeNode BuildFieldNode(DbField field) =>
        MakeNode("IconColumn", KindField, field, RefreshField, isExpanded: false);

    private static SettingsTreeNode BuildIndexNode(DbTableIndex index)
    {
        var node = MakeNode(index.PrimaryKey ? "IconLock" : "IconKey", KindIndex, index, RefreshIndex, isExpanded: false);
        if (index.IndexFields is { } ifs)
            foreach (var ifld in ifs)
                node.AddChild(BuildIndexFieldNode(ifld));
        return node;
    }

    private static SettingsTreeNode BuildIndexFieldNode(IndexField indexField) =>
        MakeNode("IconDot", KindIndexField, indexField, RefreshIndexField, isExpanded: false);

    private static void RefreshRoot(SettingsTreeNode node)
    {
        var s = (TableSchema)node.Payload!;
        node.Header = $"{s.TableName}  —  {s.DisplayName}";
        node.Detail = string.Join(Environment.NewLine,
            $"TableName：{s.TableName}",
            $"DisplayName：{s.DisplayName}",
            $"Fields：{s.Fields?.Count ?? 0}，Indexes：{s.Indexes?.Count ?? 0}");
    }

    private static void RefreshFieldsGroup(SettingsTreeNode node)
    {
        var s = (TableSchema)node.Payload!;
        node.Header = $"Fields ({s.Fields?.Count ?? 0})";
        node.Detail = "資料表欄位定義。";
    }

    private static void RefreshIndexesGroup(SettingsTreeNode node)
    {
        var s = (TableSchema)node.Payload!;
        node.Header = $"Indexes ({s.Indexes?.Count ?? 0})";
        node.Detail = "資料表索引定義（含 PrimaryKey）。";
    }

    private static void RefreshField(SettingsTreeNode node)
    {
        var f = (DbField)node.Payload!;
        node.Header = $"{f.FieldName}  —  {f.Caption}";
        node.Detail = string.Join(Environment.NewLine,
            $"FieldName：{f.FieldName}",
            $"Caption：{f.Caption}",
            $"DbType：{f.DbType}",
            $"Length：{f.Length}",
            $"Precision/Scale：{f.Precision}/{f.Scale}",
            $"AllowNull：{f.AllowNull}",
            $"DefaultValue：{f.DefaultValue}");
    }

    private static void RefreshIndex(SettingsTreeNode node)
    {
        var i = (DbTableIndex)node.Payload!;
        node.Header = $"{i.Name}  ({(i.PrimaryKey ? "PK" : i.Unique ? "Unique" : "Index")})";
        node.Detail = string.Join(Environment.NewLine,
            $"Name：{i.Name}",
            $"PrimaryKey：{i.PrimaryKey}",
            $"Unique：{i.Unique}",
            $"IndexFields：{i.IndexFields?.Count ?? 0}");
    }

    private static void RefreshIndexField(SettingsTreeNode node)
    {
        var i = (IndexField)node.Payload!;
        node.Header = $"{i.FieldName}  {i.SortDirection}";
        node.Detail = $"FieldName：{i.FieldName}\nSortDirection：{i.SortDirection}";
    }

    private static SettingsTreeNode MakeNode(
        string icon, string kind, object payload,
        Action<SettingsTreeNode> refresher, bool isExpanded)
    {
        var node = new SettingsTreeNode
        {
            Icon = icon, Kind = kind, Payload = payload,
            IsExpanded = isExpanded, Refresher = refresher,
        };
        node.RefreshDisplay();
        return node;
    }

    [RelayCommand(CanExecute = nameof(CanAddField))]
    private void AddField()
    {
        var groupNode = FindAncestor(SelectedTreeNode, KindFieldsGroup)
                        ?? Roots[0].Children.FirstOrDefault(c => c.Kind == KindFieldsGroup);
        if (groupNode is null) return;
        var name = UniqueKey(Root.Fields!.Select(f => f.FieldName), "new_field");
        var field = new DbField(name, "新欄位", FieldDbType.String);
        Root.Fields!.Add(field);
        var node = BuildFieldNode(field);
        groupNode.AddChild(node);
        groupNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "DbField", name);
    }
    private bool CanAddField() => SelectedTreeNode is not null;

    [RelayCommand(CanExecute = nameof(CanAddIndex))]
    private void AddIndex()
    {
        var groupNode = FindAncestor(SelectedTreeNode, KindIndexesGroup)
                        ?? Roots[0].Children.FirstOrDefault(c => c.Kind == KindIndexesGroup);
        if (groupNode is null) return;
        var name = UniqueKey(Root.Indexes!.Select(ix => ix.Name), "IX_new");
        var index = new DbTableIndex { Name = name };
        Root.Indexes!.Add(index);
        var node = BuildIndexNode(index);
        groupNode.AddChild(node);
        groupNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "DbTableIndex", name);
    }
    private bool CanAddIndex() => SelectedTreeNode is not null;

    [RelayCommand(CanExecute = nameof(CanAddIndexField))]
    private void AddIndexField()
    {
        var indexNode = FindAncestor(SelectedTreeNode, KindIndex);
        if (indexNode?.Payload is not DbTableIndex index) return;
        var first = (Root.Fields ?? Enumerable.Empty<DbField>()).FirstOrDefault();
        var fname = first?.FieldName ?? "field";
        var existing = (index.IndexFields ?? Enumerable.Empty<IndexField>()).Select(i => i.FieldName);
        var key = UniqueKey(existing, fname);
        var ifld = new IndexField(key, SortDirection.Asc);
        index.IndexFields!.Add(ifld);
        var node = BuildIndexFieldNode(ifld);
        indexNode.AddChild(node);
        indexNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "IndexField", key);
    }
    private bool CanAddIndexField() => FindAncestor(SelectedTreeNode, KindIndex) is not null;

    protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
    {
        KindField when node.Payload is DbField f => () => Root.Fields!.Remove(f),
        KindIndex when node.Payload is DbTableIndex ix => () => Root.Indexes!.Remove(ix),
        KindIndexField when node.Payload is IndexField ifld
            && node.Parent?.Payload is DbTableIndex parentIx
            => () => parentIx.IndexFields!.Remove(ifld),
        _ => null,
    };

    protected override IReadOnlyList<ValidationIssue> PerformValidation()
    {
        var issues = new List<ValidationIssue>();
        if (string.IsNullOrWhiteSpace(Root.TableName))
            issues.Add(new(ValidationSeverity.Error, "TableSchema", "TableName 不可為空。"));

        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in Root.Fields ?? Enumerable.Empty<DbField>())
        {
            var path = string.IsNullOrEmpty(field.FieldName) ? "Fields[?]" : $"Fields.{field.FieldName}";
            if (string.IsNullOrWhiteSpace(field.FieldName))
                issues.Add(new(ValidationSeverity.Error, path, "DbField.FieldName 不可為空。"));
            else if (!fieldNames.Add(field.FieldName))
                issues.Add(new(ValidationSeverity.Error, path, $"FieldName '{field.FieldName}' 重複。"));
        }

        var indexNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int pkCount = 0;
        foreach (var index in Root.Indexes ?? Enumerable.Empty<DbTableIndex>())
        {
            var ixPath = string.IsNullOrEmpty(index.Name) ? "Indexes[?]" : $"Indexes.{index.Name}";
            if (string.IsNullOrWhiteSpace(index.Name))
                issues.Add(new(ValidationSeverity.Error, ixPath, "DbTableIndex.Name 不可為空。"));
            else if (!indexNames.Add(index.Name))
                issues.Add(new(ValidationSeverity.Error, ixPath, $"Index name '{index.Name}' 重複。"));
            if (index.PrimaryKey) pkCount++;

            foreach (var ifld in index.IndexFields ?? Enumerable.Empty<IndexField>())
            {
                var ifPath = $"{ixPath}.{ifld.FieldName}";
                if (string.IsNullOrWhiteSpace(ifld.FieldName))
                    issues.Add(new(ValidationSeverity.Error, ifPath, "IndexField.FieldName 不可為空。"));
                else if (!fieldNames.Contains(ifld.FieldName))
                    issues.Add(new(ValidationSeverity.Error, ifPath,
                        $"IndexField '{ifld.FieldName}' 在 Fields 內不存在。"));
            }
        }
        if (pkCount > 1)
            issues.Add(new(ValidationSeverity.Error, "Indexes",
                $"PrimaryKey 索引數量為 {pkCount}，僅允許 0 或 1 個。"));
        return issues;
    }
}
