using Bee.Definition.Collections;
using Bee.Definition.Forms;

namespace Bee.DefineEditor.Models;

/// <summary>
/// Computes the (Header, Detail) display tuple a <see cref="SettingsTreeNode"/>
/// in the FormSchema tree shows for its payload. Centralised so node refresh
/// and initial build share the same formatting.
/// </summary>
internal static class FormSchemaNodeDisplay
{
    public static (string Header, string? Detail) For(string kind, object? payload) =>
        (kind, payload) switch
        {
            (FormSchemaKinds.Schema, FormSchema s) => Schema(s),
            (FormSchemaKinds.Table, FormTable t) => Table(t),
            (FormSchemaKinds.Field, FormField f) => Field(f),
            (FormSchemaKinds.Mapping, FieldMapping m) => Mapping(m),
            (FormSchemaKinds.ListItem, ListItem i) => ListItem(i),
            (FormSchemaKinds.RelationGroup, FormField f) => ("Relation", $"RelationProgId：{f.RelationProgId}"),
            (FormSchemaKinds.LookupGroup, FormField f) => ("Lookup", $"LookupProgId：{f.LookupProgId}"),
            (FormSchemaKinds.ListItemsGroup, FormField f) => ListItemsGroup(f),
            _ => (string.Empty, null),
        };

    private static (string, string) Schema(FormSchema s) => (
        $"{s.ProgId}  —  {s.DisplayName}",
        string.Join(Environment.NewLine,
            $"ProgId：{s.ProgId}",
            $"DisplayName：{s.DisplayName}",
            $"CategoryId：{s.CategoryId}",
            $"ListFields：{s.ListFields}",
            $"PermissionModelId：{s.PermissionModelId}"));

    private static (string, string) Table(FormTable t) => (
        $"{t.TableName}  —  {t.DisplayName}",
        string.Join(Environment.NewLine,
            $"TableName：{t.TableName}",
            $"DbTableName：{t.DbTableName}",
            $"DisplayName：{t.DisplayName}"));

    private static (string, string) Field(FormField f) => (
        $"{f.FieldName}  —  {f.Caption}",
        string.Join(Environment.NewLine,
            $"FieldName：{f.FieldName}",
            $"Caption：{f.Caption}",
            $"DbType：{f.DbType}",
            $"Type：{f.Type}",
            $"ControlType：{f.ControlType}",
            $"MaxLength：{f.MaxLength}",
            $"DefaultValue：{f.DefaultValue}",
            $"Visible：{f.Visible}",
            $"ScopeRole：{f.ScopeRole}"));

    private static (string, string) Mapping(FieldMapping m) => (
        $"{m.SourceField}  →  {m.DestinationField}",
        string.Join(Environment.NewLine,
            $"SourceField：{m.SourceField}",
            $"DestinationField：{m.DestinationField}"));

    private static (string, string) ListItem(ListItem i) => (
        $"{i.Value}  —  {i.Text}",
        string.Join(Environment.NewLine,
            $"Value：{i.Value}",
            $"Text：{i.Text}"));

    private static (string, string) ListItemsGroup(FormField f) => (
        $"ListItems ({f.ListItems?.Count ?? 0})",
        string.IsNullOrEmpty(f.LangEnumName)
            ? "(Static ListItems)"
            : $"LangEnumName：{f.LangEnumName} (overrides static items)");
}
