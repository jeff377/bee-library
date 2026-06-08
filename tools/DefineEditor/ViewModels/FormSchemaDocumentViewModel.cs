using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Collections;
using Bee.Definition.Forms;
using Bee.DefineEditor.Models;
using Bee.DefineEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// FormSchema editor. Loads the schema via <see cref="XmlCodec"/>, exposes an
/// inner tree, lets the right-pane DataTemplates two-way bind to the underlying
/// Bee.Definition payload, and offers Save / Add / Delete / Validate commands
/// that mutate the in-memory schema and its persisted XML.
/// </summary>
public sealed partial class FormSchemaDocumentViewModel : DocumentViewModelBase
{
    public override string Title { get; }

    public override string DocumentKey => FilePath;

    public override string TabIcon => "DefFormSchema";

    // Forwarders for the base type's uniform file-level command surface — see
    // DocumentViewModelBase.FileSaveCommand. The generated SaveCommand /
    // ValidateCommand below carry the actual logic; this just exposes them
    // under a name the source generator hasn't taken.
    public override IRelayCommand FileSaveCommand => SaveCommand;
    public override IRelayCommand FileValidateCommand => ValidateCommand;

    public string FilePath { get; }

    public FormSchema Schema { get; }

    public SolutionContext Solution { get; }

    public ObservableCollection<FormSchemaTreeNode> Roots { get; } = new();

    public ObservableCollection<ValidationIssue> Issues { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedEditorContext))]
    [NotifyPropertyChangedFor(nameof(SelectedKindIsSchema))]
    [NotifyPropertyChangedFor(nameof(SelectedKindIsTable))]
    [NotifyPropertyChangedFor(nameof(SelectedKindIsField))]
    [NotifyPropertyChangedFor(nameof(SelectedKindIsRelationGroup))]
    [NotifyPropertyChangedFor(nameof(SelectedKindIsLookupGroup))]
    [NotifyPropertyChangedFor(nameof(SelectedKindIsListItemsGroup))]
    [NotifyPropertyChangedFor(nameof(SelectedKindCanDelete))]
    [NotifyCanExecuteChangedFor(nameof(AddTableCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddFieldCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddRelationMappingCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddLookupMappingCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddListItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private FormSchemaTreeNode? _selectedTreeNode;

    // Visibility hints for the tree-view context menu. Each MenuItem binds
    // IsVisible to the flag matching the kind it applies to. Kept here rather
    // than on the node model so the VM owns "what can be done from this kind"
    // — the node stays a passive data holder.
    public bool SelectedKindIsSchema => SelectedTreeNode?.Kind == FormSchemaNodeKind.Schema;
    public bool SelectedKindIsTable => SelectedTreeNode?.Kind == FormSchemaNodeKind.Table;
    public bool SelectedKindIsField => SelectedTreeNode?.Kind == FormSchemaNodeKind.Field;
    public bool SelectedKindIsRelationGroup => SelectedTreeNode?.Kind == FormSchemaNodeKind.RelationGroup;
    public bool SelectedKindIsLookupGroup => SelectedTreeNode?.Kind == FormSchemaNodeKind.LookupGroup;
    public bool SelectedKindIsListItemsGroup => SelectedTreeNode?.Kind == FormSchemaNodeKind.ListItemsGroup;
    public bool SelectedKindCanDelete => SelectedTreeNode?.Kind is
        FormSchemaNodeKind.Table or
        FormSchemaNodeKind.Field or
        FormSchemaNodeKind.Mapping or
        FormSchemaNodeKind.ListItem;

    // IsDirty / StatusText / culture-change refresh are inherited from
    // DocumentViewModelBase.

    /// <summary>
    /// Content shown in the right-pane <see cref="Avalonia.Controls.ContentControl"/>.
    /// Group nodes (Relation / Lookup) yield a dedicated <see cref="MappingGroupEditor"/>
    /// wrapper; everything else yields the raw payload so the existing
    /// FormSchema / FormTable / FormField / FieldMapping / ListItem templates apply.
    /// </summary>
    public object? SelectedEditorContext => SelectedTreeNode is null
        ? null
        : SelectedTreeNode.Kind switch
        {
            FormSchemaNodeKind.RelationGroup when SelectedTreeNode.Payload is FormField rf =>
                new MappingGroupEditor(rf, isRelation: true, Solution.AvailableProgIds),
            FormSchemaNodeKind.LookupGroup when SelectedTreeNode.Payload is FormField lf =>
                new MappingGroupEditor(lf, isRelation: false, Solution.AvailableProgIds),
            _ => SelectedTreeNode.Payload,
        };

    private FormSchemaDocumentViewModel(string filePath, FormSchema schema, SolutionContext solution)
    {
        FilePath = filePath;
        Schema = schema;
        Solution = solution;
        Title = $"FormSchema — {schema.ProgId}";
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

    [RelayCommand]
    private async System.Threading.Tasks.Task Save()
    {
        try
        {
            var issues = Bee.DefineEditor.Services.FormSchemaValidator.Validate(Schema, Solution);
            if (!await ConfirmSaveAfterValidationAsync(issues, Issues))
            {
                var errs = issues.Count(i => i.Severity == ValidationSeverity.Error);
                StatusText = L("Status_SaveCancelled", errs);
                return;
            }

            foreach (var root in Roots)
                root.RefreshRecursive();
            XmlCodec.SerializeToFile(Schema, FilePath);
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
        var found = FormSchemaValidator.Validate(Schema, Solution);
        foreach (var issue in found)
            Issues.Add(issue);
        if (Issues.Count == 0)
        {
            StatusText = L("Status_ValidationPassed");
        }
        else
        {
            var errors = found.Count(i => i.Severity == ValidationSeverity.Error);
            var warnings = found.Count(i => i.Severity == ValidationSeverity.Warning);
            StatusText = L("Status_ValidationCompleted", Issues.Count, errors, warnings);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddTable))]
    private void AddTable()
    {
        if (SelectedTreeNode is not { Kind: FormSchemaNodeKind.Schema, Payload: FormSchema schema } schemaNode)
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

    private bool CanAddTable() => SelectedTreeNode?.Kind == FormSchemaNodeKind.Schema;

    [RelayCommand(CanExecute = nameof(CanAddField))]
    private void AddField()
    {
        var tableNode = FindAncestor(SelectedTreeNode, FormSchemaNodeKind.Table);
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
        FindAncestor(SelectedTreeNode, FormSchemaNodeKind.Table) is not null;

    [RelayCommand(CanExecute = nameof(CanAddRelationMapping))]
    private void AddRelationMapping()
    {
        var fieldNode = FindAncestor(SelectedTreeNode, FormSchemaNodeKind.Field);
        if (fieldNode?.Payload is not FormField field) return;

        var mapping = new FieldMapping(string.Empty, string.Empty);
        field.RelationFieldMappings!.Add(mapping);

        var group = EnsureGroup(fieldNode, FormSchemaNodeKind.RelationGroup,
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
        FindAncestor(SelectedTreeNode, FormSchemaNodeKind.Field) is not null;

    [RelayCommand(CanExecute = nameof(CanAddLookupMapping))]
    private void AddLookupMapping()
    {
        var fieldNode = FindAncestor(SelectedTreeNode, FormSchemaNodeKind.Field);
        if (fieldNode?.Payload is not FormField field) return;

        var mapping = new FieldMapping(string.Empty, string.Empty);
        field.LookupFieldMappings!.Add(mapping);

        var group = EnsureGroup(fieldNode, FormSchemaNodeKind.LookupGroup,
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
        FindAncestor(SelectedTreeNode, FormSchemaNodeKind.Field) is not null;

    [RelayCommand(CanExecute = nameof(CanAddListItem))]
    private void AddListItem()
    {
        var fieldNode = FindAncestor(SelectedTreeNode, FormSchemaNodeKind.Field);
        if (fieldNode?.Payload is not FormField field) return;

        var key = UniqueKey((field.ListItems ?? new ListItemCollection()).Select(i => i.Value), "value");
        var item = new ListItem(key, "New option");
        field.ListItems!.Add(item);

        var group = EnsureGroup(fieldNode, FormSchemaNodeKind.ListItemsGroup,
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
        FindAncestor(SelectedTreeNode, FormSchemaNodeKind.Field) is not null;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async System.Threading.Tasks.Task Delete()
    {
        var node = SelectedTreeNode;
        if (node is null) return;

        if (!await ConfirmDeleteAsync(node.Header))
        {
            StatusText = L("Status_DeleteCancelled");
            return;
        }

        switch (node.Kind)
        {
            case FormSchemaNodeKind.Table
                when node.Payload is FormTable t && node.Parent?.Payload is FormSchema s:
                s.Tables!.Remove(t);
                break;

            case FormSchemaNodeKind.Field
                when node.Payload is FormField f && node.Parent?.Payload is FormTable t:
                t.Fields!.Remove(f);
                break;

            case FormSchemaNodeKind.Mapping
                when node.Payload is FieldMapping m &&
                     node.Parent is { Kind: FormSchemaNodeKind.RelationGroup, Payload: FormField rf }:
                rf.RelationFieldMappings!.Remove(m);
                break;

            case FormSchemaNodeKind.Mapping
                when node.Payload is FieldMapping m &&
                     node.Parent is { Kind: FormSchemaNodeKind.LookupGroup, Payload: FormField lf }:
                lf.LookupFieldMappings!.Remove(m);
                break;

            case FormSchemaNodeKind.ListItem
                when node.Payload is ListItem i &&
                     node.Parent is { Kind: FormSchemaNodeKind.ListItemsGroup, Payload: FormField pf }:
                pf.ListItems!.Remove(i);
                break;

            default:
                return;
        }

        var parent = node.Parent;
        node.RemoveSelf();
        SelectedTreeNode = parent;
        IsDirty = true;
        StatusText = L("Status_Deleted");
    }

    private bool CanDelete() => SelectedTreeNode?.Kind is
        FormSchemaNodeKind.Table or
        FormSchemaNodeKind.Field or
        FormSchemaNodeKind.Mapping or
        FormSchemaNodeKind.ListItem;

    private static FormSchemaTreeNode? FindAncestor(FormSchemaTreeNode? node, FormSchemaNodeKind kind)
    {
        for (var cur = node; cur != null; cur = cur.Parent)
            if (cur.Kind == kind) return cur;
        return null;
    }

    private static FormSchemaTreeNode EnsureGroup(
        FormSchemaTreeNode fieldNode,
        FormSchemaNodeKind groupKind,
        Func<FormField, FormSchemaTreeNode> builder)
    {
        var existing = fieldNode.Children.FirstOrDefault(c => c.Kind == groupKind);
        if (existing is not null) return existing;
        var group = builder((FormField)fieldNode.Payload!);
        fieldNode.AddChild(group);
        return group;
    }

}
