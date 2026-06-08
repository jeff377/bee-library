using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bee.Base.Serialization;
using Bee.Definition.Layouts;
using Bee.DefineEditor.Models;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Editor for <see cref="FormLayout"/>. Tree: FormLayout → Sections group →
/// LayoutSection[] → LayoutField[]; FormLayout → Details group → LayoutGrid[] →
/// LayoutColumn[].
/// </summary>
public sealed partial class FormLayoutDocumentViewModel : SingletonDocumentViewModelBase
{
    public const string KindRoot = "FormLayout";
    public const string KindSectionsGroup = "SectionsGroup";
    public const string KindSection = "LayoutSection";
    public const string KindLayoutField = "LayoutField";
    public const string KindDetailsGroup = "DetailsGroup";
    public const string KindGrid = "LayoutGrid";
    public const string KindLayoutColumn = "LayoutColumn";

    public FormLayout Root { get; }
    protected override object RootObject => Root;

    public override string TabIcon => "DefFormLayout";

    public bool SelectedKindIsSectionsGroup => SelectedTreeNode?.Kind == KindSectionsGroup;
    public bool SelectedKindIsSection => SelectedTreeNode?.Kind == KindSection;
    public bool SelectedKindIsDetailsGroup => SelectedTreeNode?.Kind == KindDetailsGroup;
    public bool SelectedKindIsGrid => SelectedTreeNode?.Kind == KindGrid;

    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsSectionsGroup));
        OnPropertyChanged(nameof(SelectedKindIsSection));
        OnPropertyChanged(nameof(SelectedKindIsDetailsGroup));
        OnPropertyChanged(nameof(SelectedKindIsGrid));
    }

    private FormLayoutDocumentViewModel(string filePath, FormLayout root)
        : base(filePath, "FormLayout", keyText: root.LayoutId)
    {
        Root = root;
        Roots.Add(BuildRootNode(root));
        SelectedTreeNode = Roots[0];
    }

    public static FormLayoutDocumentViewModel Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("FormLayout file not found.", filePath);
        var root = XmlCodec.DeserializeFromFile<FormLayout>(filePath)
            ?? throw new InvalidOperationException($"FormLayout deserialized to null: {filePath}");
        return new FormLayoutDocumentViewModel(filePath, root);
    }

    private static SettingsTreeNode BuildRootNode(FormLayout root)
    {
        var rootNode = SettingsTreeNode.Create("DefFormLayout", KindRoot, root, RefreshRoot, isExpanded: true);

        var sectionsGroup = SettingsTreeNode.Create("IconSection", KindSectionsGroup, root, RefreshSectionsGroup, isExpanded: true);
        if (root.Sections is { } sections)
            foreach (var s in sections)
                sectionsGroup.AddChild(BuildSectionNode(s));
        rootNode.AddChild(sectionsGroup);

        var detailsGroup = SettingsTreeNode.Create("IconGrid", KindDetailsGroup, root, RefreshDetailsGroup, isExpanded: true);
        if (root.Details is { } details)
            foreach (var g in details)
                detailsGroup.AddChild(BuildGridNode(g));
        rootNode.AddChild(detailsGroup);

        return rootNode;
    }

    private static SettingsTreeNode BuildSectionNode(LayoutSection section)
    {
        var node = SettingsTreeNode.Create("IconSection", KindSection, section, RefreshSection, isExpanded: false);
        if (section.Fields is { } fields)
            foreach (var f in fields)
                node.AddChild(BuildLayoutFieldNode(f));
        return node;
    }

    private static SettingsTreeNode BuildLayoutFieldNode(LayoutField field) =>
        SettingsTreeNode.Create("IconText", KindLayoutField, field, RefreshLayoutField, isExpanded: false);

    private static SettingsTreeNode BuildGridNode(LayoutGrid grid)
    {
        var node = SettingsTreeNode.Create("IconGrid", KindGrid, grid, RefreshGrid, isExpanded: false);
        if (grid.Columns is { } columns)
            foreach (var c in columns)
                node.AddChild(BuildLayoutColumnNode(c));
        return node;
    }

    private static SettingsTreeNode BuildLayoutColumnNode(LayoutColumn column) =>
        SettingsTreeNode.Create("IconColumn", KindLayoutColumn, column, RefreshLayoutColumn, isExpanded: false);

    private static void RefreshRoot(SettingsTreeNode node)
    {
        var l = (FormLayout)node.Payload!;
        node.Header = $"{l.LayoutId}  —  {l.Caption}";
        node.Detail = string.Join(Environment.NewLine,
            $"LayoutId：{l.LayoutId}",
            $"ProgId：{l.ProgId}",
            $"Caption：{l.Caption}",
            $"ColumnCount：{l.ColumnCount}");
    }

    private static void RefreshSectionsGroup(SettingsTreeNode node)
    {
        var l = (FormLayout)node.Payload!;
        node.Header = $"Sections ({l.Sections?.Count ?? 0})";
        node.Detail = "Master form section (shares the ColumnCount column allocation).";
    }

    private static void RefreshDetailsGroup(SettingsTreeNode node)
    {
        var l = (FormLayout)node.Payload!;
        node.Header = $"Details ({l.Details?.Count ?? 0})";
        node.Detail = "Detail grid (full-width below the master form).";
    }

    private static void RefreshSection(SettingsTreeNode node)
    {
        var s = (LayoutSection)node.Payload!;
        node.Header = $"{s.Name}  —  {s.Caption}";
        node.Detail = string.Join(Environment.NewLine,
            $"Name：{s.Name}",
            $"Caption：{s.Caption}",
            $"ShowCaption：{s.ShowCaption}",
            $"Fields：{s.Fields?.Count ?? 0}");
    }

    private static void RefreshLayoutField(SettingsTreeNode node)
    {
        var f = (LayoutField)node.Payload!;
        node.Header = $"{f.FieldName}  —  {f.Caption}";
        node.Detail = string.Join(Environment.NewLine,
            $"FieldName：{f.FieldName}",
            $"Caption：{f.Caption}",
            $"ControlType：{f.ControlType}",
            $"RowSpan/ColumnSpan：{f.RowSpan}/{f.ColumnSpan}",
            $"Visible：{f.Visible}",
            $"ReadOnly：{f.ReadOnly}");
    }

    private static void RefreshGrid(SettingsTreeNode node)
    {
        var g = (LayoutGrid)node.Payload!;
        node.Header = $"{g.TableName}  —  {g.Caption}";
        node.Detail = string.Join(Environment.NewLine,
            $"TableName：{g.TableName}",
            $"Caption：{g.Caption}",
            $"AllowActions：{g.AllowActions}",
            $"Columns：{g.Columns?.Count ?? 0}");
    }

    private static void RefreshLayoutColumn(SettingsTreeNode node)
    {
        var c = (LayoutColumn)node.Payload!;
        node.Header = $"{c.FieldName}  —  {c.Caption}";
        node.Detail = string.Join(Environment.NewLine,
            $"FieldName：{c.FieldName}",
            $"Caption：{c.Caption}",
            $"ControlType：{c.ControlType}",
            $"Width：{c.Width}",
            $"Visible：{c.Visible}",
            $"ReadOnly：{c.ReadOnly}");
    }

    [RelayCommand(CanExecute = nameof(CanAddSection))]
    private void AddSection()
    {
        var groupNode = FindAncestor(SelectedTreeNode, KindSectionsGroup)
                        ?? Roots[0].Children.FirstOrDefault(c => c.Kind == KindSectionsGroup);
        if (groupNode is null) return;
        var name = UniqueKey(
            (Root.Sections ?? new LayoutSectionCollection()).Select(s => s.Name),
            "Section");
        var section = new LayoutSection { Name = name, Caption = "New section" };
        Root.Sections!.Add(section);
        var node = BuildSectionNode(section);
        groupNode.AddChild(node);
        groupNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "LayoutSection", name);
    }
    private bool CanAddSection() => SelectedTreeNode is not null;

    [RelayCommand(CanExecute = nameof(CanAddLayoutField))]
    private void AddLayoutField()
    {
        var sectionNode = FindAncestor(SelectedTreeNode, KindSection);
        if (sectionNode?.Payload is not LayoutSection section) return;
        var name = UniqueKey(
            (section.Fields ?? new LayoutFieldCollection()).Select(f => f.FieldName),
            "new_field");
        var field = new LayoutField { FieldName = name, Caption = "New field" };
        section.Fields!.Add(field);
        var node = BuildLayoutFieldNode(field);
        sectionNode.AddChild(node);
        sectionNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "LayoutField", name);
    }
    private bool CanAddLayoutField() => FindAncestor(SelectedTreeNode, KindSection) is not null;

    [RelayCommand(CanExecute = nameof(CanAddGrid))]
    private void AddGrid()
    {
        var groupNode = FindAncestor(SelectedTreeNode, KindDetailsGroup)
                        ?? Roots[0].Children.FirstOrDefault(c => c.Kind == KindDetailsGroup);
        if (groupNode is null) return;
        var name = UniqueKey(
            (Root.Details ?? new LayoutGridCollection()).Select(g => g.TableName),
            "DetailTable");
        var grid = new LayoutGrid(name, "New detail grid");
        Root.Details!.Add(grid);
        var node = BuildGridNode(grid);
        groupNode.AddChild(node);
        groupNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "LayoutGrid", name);
    }
    private bool CanAddGrid() => SelectedTreeNode is not null;

    [RelayCommand(CanExecute = nameof(CanAddLayoutColumn))]
    private void AddLayoutColumn()
    {
        var gridNode = FindAncestor(SelectedTreeNode, KindGrid);
        if (gridNode?.Payload is not LayoutGrid grid) return;
        var name = UniqueKey(
            (grid.Columns ?? new LayoutColumnCollection()).Select(c => c.FieldName),
            "new_column");
        var column = new LayoutColumn { FieldName = name, Caption = "New field" };
        grid.Columns!.Add(column);
        var node = BuildLayoutColumnNode(column);
        gridNode.AddChild(node);
        gridNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "LayoutColumn", name);
    }
    private bool CanAddLayoutColumn() => FindAncestor(SelectedTreeNode, KindGrid) is not null;

    protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
    {
        KindSection when node.Payload is LayoutSection s => () => Root.Sections!.Remove(s),
        KindLayoutField when node.Payload is LayoutField f
            && node.Parent?.Payload is LayoutSection parentSection
            => () => parentSection.Fields!.Remove(f),
        KindGrid when node.Payload is LayoutGrid g => () => Root.Details!.Remove(g),
        KindLayoutColumn when node.Payload is LayoutColumn c
            && node.Parent?.Payload is LayoutGrid parentGrid
            => () => parentGrid.Columns!.Remove(c),
        _ => null,
    };

    protected override IReadOnlyList<ValidationIssue> PerformValidation()
    {
        var issues = new List<ValidationIssue>();
        if (string.IsNullOrWhiteSpace(Root.LayoutId))
            issues.Add(new(ValidationSeverity.Error, "FormLayout", "LayoutId cannot be empty."));
        if (string.IsNullOrWhiteSpace(Root.ProgId))
            issues.Add(new(ValidationSeverity.Error, "FormLayout", "ProgId cannot be empty."));
        if (Root.ColumnCount <= 0)
            issues.Add(new(ValidationSeverity.Error, "FormLayout",
                $"ColumnCount must be greater than 0 (current value: {Root.ColumnCount})."));

        var sectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in Root.Sections ?? Enumerable.Empty<LayoutSection>())
        {
            var sPath = string.IsNullOrEmpty(section.Name) ? "Sections[?]" : $"Sections.{section.Name}";
            if (string.IsNullOrWhiteSpace(section.Name))
                issues.Add(new(ValidationSeverity.Error, sPath, "LayoutSection.Name cannot be empty."));
            else if (!sectionNames.Add(section.Name))
                issues.Add(new(ValidationSeverity.Error, sPath,
                    $"Section.Name '{section.Name}' is a duplicate."));

            var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in section.Fields ?? Enumerable.Empty<LayoutField>())
            {
                var fPath = $"{sPath}.{(string.IsNullOrEmpty(f.FieldName) ? "(unnamed)" : f.FieldName)}";
                if (string.IsNullOrWhiteSpace(f.FieldName))
                    issues.Add(new(ValidationSeverity.Error, fPath, "LayoutField.FieldName cannot be empty."));
                else if (!fieldNames.Add(f.FieldName))
                    issues.Add(new(ValidationSeverity.Error, fPath,
                        $"LayoutField.FieldName '{f.FieldName}' is a duplicate within '{section.Name}'."));
            }
        }

        var gridNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var grid in Root.Details ?? Enumerable.Empty<LayoutGrid>())
        {
            var gPath = string.IsNullOrEmpty(grid.TableName) ? "Details[?]" : $"Details.{grid.TableName}";
            if (string.IsNullOrWhiteSpace(grid.TableName))
                issues.Add(new(ValidationSeverity.Error, gPath, "LayoutGrid.TableName cannot be empty."));
            else if (!gridNames.Add(grid.TableName))
                issues.Add(new(ValidationSeverity.Error, gPath,
                    $"LayoutGrid.TableName '{grid.TableName}' is a duplicate."));

            var colNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in grid.Columns ?? Enumerable.Empty<LayoutColumn>())
            {
                var cPath = $"{gPath}.{(string.IsNullOrEmpty(c.FieldName) ? "(unnamed)" : c.FieldName)}";
                if (string.IsNullOrWhiteSpace(c.FieldName))
                    issues.Add(new(ValidationSeverity.Error, cPath, "LayoutColumn.FieldName cannot be empty."));
                else if (!colNames.Add(c.FieldName))
                    issues.Add(new(ValidationSeverity.Error, cPath,
                        $"LayoutColumn.FieldName '{c.FieldName}' is a duplicate within '{grid.TableName}'."));
            }
        }
        return issues;
    }
}
