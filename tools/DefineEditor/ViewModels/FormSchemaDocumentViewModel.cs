using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Collections;
using Bee.Definition.Forms;
using Bee.DefineEditor.Models;
using Bee.DefineEditor.Services;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// FormSchema editor. Loads the schema via <see cref="XmlCodec"/>, exposes an
/// inner tree, lets the right-pane DataTemplates two-way bind to the underlying
/// Bee.Definition payload, and offers Add commands for tables / fields /
/// mappings / list items. Save / Validate / Delete and tree-node plumbing
/// inherit from <see cref="SingletonDocumentViewModelBase"/>.
/// </summary>
public sealed partial class FormSchemaDocumentViewModel : SingletonDocumentViewModelBase
{
    public override string TabIcon => "DefFormSchema";

    public FormSchema Schema { get; }

    public SolutionContext Solution { get; }

    /// <summary>Backing object handed to <see cref="XmlCodec.SerializeToFile"/> by base's Save.</summary>
    protected override object RootObject => Schema;

    // Visibility hints for the tree-view context menu. Each MenuItem binds
    // IsVisible to the flag matching the kind it applies to. Kept here rather
    // than on the node model so the VM owns "what can be done from this kind"
    // — the node stays a passive data holder.
    public bool SelectedKindIsSchema => SelectedTreeNode?.Kind == FormSchemaKinds.Schema;
    public bool SelectedKindIsTable => SelectedTreeNode?.Kind == FormSchemaKinds.Table;
    public bool SelectedKindIsField => SelectedTreeNode?.Kind == FormSchemaKinds.Field;
    public bool SelectedKindIsRelationGroup => SelectedTreeNode?.Kind == FormSchemaKinds.RelationGroup;
    public bool SelectedKindIsLookupGroup => SelectedTreeNode?.Kind == FormSchemaKinds.LookupGroup;
    public bool SelectedKindIsListItemsGroup => SelectedTreeNode?.Kind == FormSchemaKinds.ListItemsGroup;

    /// <summary>
    /// Content shown in the right-pane <see cref="Avalonia.Controls.ContentControl"/>.
    /// Group nodes (Relation / Lookup) yield a dedicated <see cref="MappingGroupEditor"/>
    /// wrapper; everything else yields the raw payload so the existing
    /// FormSchema / FormTable / FormField / FieldMapping / ListItem templates apply.
    /// </summary>
    public override object? SelectedEditorContext => SelectedTreeNode is null
        ? null
        : SelectedTreeNode.Kind switch
        {
            FormSchemaKinds.RelationGroup when SelectedTreeNode.Payload is FormField rf =>
                new MappingGroupEditor(rf, isRelation: true, Solution.AvailableProgIds),
            FormSchemaKinds.LookupGroup when SelectedTreeNode.Payload is FormField lf =>
                new MappingGroupEditor(lf, isRelation: false, Solution.AvailableProgIds),
            _ => SelectedTreeNode.Payload,
        };

    private FormSchemaDocumentViewModel(string filePath, FormSchema schema, SolutionContext solution)
        : base(filePath, "FormSchema", schema.ProgId)
    {
        Schema = schema;
        Solution = solution;
        var root = FormSchemaNodeBuilder.BuildSchema(schema);
        Roots.Add(root);
        SelectedTreeNode = root;
    }

    public static FormSchemaDocumentViewModel Load(string filePath, SolutionContext solution)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("FormSchema file not found.", filePath);

        var schema = XmlCodec.DeserializeFromFile<FormSchema>(filePath)
            ?? throw new InvalidOperationException($"FormSchema deserialized to null: {filePath}");
        return new FormSchemaDocumentViewModel(filePath, schema, solution);
    }

    protected override IReadOnlyList<ValidationIssue> PerformValidation()
        => FormSchemaValidator.Validate(Schema, Solution);

    /// <summary>
    /// Base fires the SelectedEditorContext / SelectedKindCanDelete / DeleteCommand
    /// notifications itself via the <c>[NotifyXxx]</c> attributes on its
    /// <c>_selectedTreeNode</c>. This override adds FormSchema-specific fan-out:
    /// the six <c>SelectedKindIsX</c> context-menu visibility flags and the five
    /// AddXxxCommand can-execute states (which can't be wired through
    /// <c>[NotifyCanExecuteChangedFor]</c> from base's field).
    /// </summary>
    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsSchema));
        OnPropertyChanged(nameof(SelectedKindIsTable));
        OnPropertyChanged(nameof(SelectedKindIsField));
        OnPropertyChanged(nameof(SelectedKindIsRelationGroup));
        OnPropertyChanged(nameof(SelectedKindIsLookupGroup));
        OnPropertyChanged(nameof(SelectedKindIsListItemsGroup));
        AddTableCommand.NotifyCanExecuteChanged();
        AddFieldCommand.NotifyCanExecuteChanged();
        AddRelationMappingCommand.NotifyCanExecuteChanged();
        AddLookupMappingCommand.NotifyCanExecuteChanged();
        AddListItemCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddTable))]
    private void AddTable()
    {
        if (SelectedTreeNode is not { Kind: FormSchemaKinds.Schema, Payload: FormSchema schema } schemaNode)
            return;

        var name = UniqueKey(schema.Tables!.Select(t => t.TableName), "NewTable");
        var table = new FormTable(name, "New table");
        schema.Tables!.Add(table);

        var node = FormSchemaNodeBuilder.BuildTable(table);
        schemaNode.AddChild(node);
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "FormTable", name);
    }

    private bool CanAddTable() => SelectedTreeNode?.Kind == FormSchemaKinds.Schema;

    [RelayCommand(CanExecute = nameof(CanAddField))]
    private void AddField()
    {
        var tableNode = FindAncestor(SelectedTreeNode, FormSchemaKinds.Table);
        if (tableNode?.Payload is not FormTable table) return;

        var name = UniqueKey(table.Fields!.Select(f => f.FieldName), "new_field");
        var field = new FormField(name, "New field", FieldDbType.String);
        table.Fields!.Add(field);

        var node = FormSchemaNodeBuilder.BuildField(field);
        tableNode.AddChild(node);
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "FormField", name);
    }

    private bool CanAddField() =>
        FindAncestor(SelectedTreeNode, FormSchemaKinds.Table) is not null;

    [RelayCommand(CanExecute = nameof(CanAddRelationMapping))]
    private void AddRelationMapping()
    {
        var fieldNode = FindAncestor(SelectedTreeNode, FormSchemaKinds.Field);
        if (fieldNode?.Payload is not FormField field) return;

        var mapping = new FieldMapping(string.Empty, string.Empty);
        field.RelationFieldMappings!.Add(mapping);

        var group = EnsureGroup(fieldNode, FormSchemaKinds.RelationGroup,
            f => FormSchemaNodeBuilder.BuildRelationGroup(f));
        var node = FormSchemaNodeBuilder.BuildMapping(mapping);
        group.AddChild(node);
        group.IsExpanded = true;
        fieldNode.IsExpanded = true;

        // If user was sitting on the relation group, refresh the editor view of
        // the group so the new mapping appears immediately; otherwise focus the
        // new mapping for inline editing.
        if (SelectedTreeNode == group)
            OnPropertyChanged(nameof(SelectedEditorContext));
        else
            SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "Relation FieldMapping", "");
    }

    private bool CanAddRelationMapping() =>
        FindAncestor(SelectedTreeNode, FormSchemaKinds.Field) is not null;

    [RelayCommand(CanExecute = nameof(CanAddLookupMapping))]
    private void AddLookupMapping()
    {
        var fieldNode = FindAncestor(SelectedTreeNode, FormSchemaKinds.Field);
        if (fieldNode?.Payload is not FormField field) return;

        var mapping = new FieldMapping(string.Empty, string.Empty);
        field.LookupFieldMappings!.Add(mapping);

        var group = EnsureGroup(fieldNode, FormSchemaKinds.LookupGroup,
            f => FormSchemaNodeBuilder.BuildLookupGroup(f));
        var node = FormSchemaNodeBuilder.BuildMapping(mapping);
        group.AddChild(node);
        group.IsExpanded = true;
        fieldNode.IsExpanded = true;

        if (SelectedTreeNode == group)
            OnPropertyChanged(nameof(SelectedEditorContext));
        else
            SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "Lookup FieldMapping", "");
    }

    private bool CanAddLookupMapping() =>
        FindAncestor(SelectedTreeNode, FormSchemaKinds.Field) is not null;

    [RelayCommand(CanExecute = nameof(CanAddListItem))]
    private void AddListItem()
    {
        var fieldNode = FindAncestor(SelectedTreeNode, FormSchemaKinds.Field);
        if (fieldNode?.Payload is not FormField field) return;

        var key = UniqueKey((field.ListItems ?? new ListItemCollection()).Select(i => i.Value), "value");
        var item = new ListItem(key, "New option");
        field.ListItems!.Add(item);

        var group = EnsureGroup(fieldNode, FormSchemaKinds.ListItemsGroup,
            f => FormSchemaNodeBuilder.BuildListItemsGroup(f));
        var node = FormSchemaNodeBuilder.BuildListItem(item);
        group.AddChild(node);
        group.IsExpanded = true;
        fieldNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "ListItem", key);
    }

    private bool CanAddListItem() =>
        FindAncestor(SelectedTreeNode, FormSchemaKinds.Field) is not null;

    protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
    {
        FormSchemaKinds.Table when node.Payload is FormTable t
            && node.Parent?.Payload is FormSchema s
            => () => s.Tables!.Remove(t),
        FormSchemaKinds.Field when node.Payload is FormField f
            && node.Parent?.Payload is FormTable t
            => () => t.Fields!.Remove(f),
        FormSchemaKinds.Mapping when node.Payload is FieldMapping m
            && node.Parent is { Kind: FormSchemaKinds.RelationGroup, Payload: FormField rf }
            => () => rf.RelationFieldMappings!.Remove(m),
        FormSchemaKinds.Mapping when node.Payload is FieldMapping m
            && node.Parent is { Kind: FormSchemaKinds.LookupGroup, Payload: FormField lf }
            => () => lf.LookupFieldMappings!.Remove(m),
        FormSchemaKinds.ListItem when node.Payload is ListItem i
            && node.Parent is { Kind: FormSchemaKinds.ListItemsGroup, Payload: FormField pf }
            => () => pf.ListItems!.Remove(i),
        _ => null,
    };

    private static SettingsTreeNode EnsureGroup(
        SettingsTreeNode fieldNode,
        string groupKind,
        Func<FormField, SettingsTreeNode> builder)
    {
        var existing = fieldNode.Children.FirstOrDefault(c => c.Kind == groupKind);
        if (existing is not null) return existing;
        var group = builder((FormField)fieldNode.Payload!);
        fieldNode.AddChild(group);
        return group;
    }
}
