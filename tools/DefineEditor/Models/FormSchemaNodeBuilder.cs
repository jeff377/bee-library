using Bee.Definition.Database;
using Bee.Definition.Forms;

namespace Bee.DefineEditor.Models;

/// <summary>
/// Builds <see cref="SettingsTreeNode"/> instances from Bee.Definition payloads
/// for the FormSchema inner tree. Each builder sets payload + icon + initial
/// expansion + a shared <see cref="Refresh"/> delegate that defers to
/// <see cref="FormSchemaNodeDisplay"/> for header/detail formatting.
/// </summary>
internal static class FormSchemaNodeBuilder
{
    public static SettingsTreeNode BuildSchema(FormSchema schema)
    {
        var node = Make(FormSchemaKinds.Schema, "DefFormSchema", schema, isExpanded: true);
        if (schema.Tables is { } tables)
            foreach (var table in tables)
                node.AddChild(BuildTable(table));
        return node;
    }

    public static SettingsTreeNode BuildTable(FormTable table)
    {
        var node = Make(FormSchemaKinds.Table, "IconTable", table, isExpanded: true);
        if (table.Fields is { } fields)
            foreach (var field in fields)
                node.AddChild(BuildField(field));
        return node;
    }

    public static SettingsTreeNode BuildField(FormField field)
    {
        var node = Make(FormSchemaKinds.Field, FieldIcon(field), field, isExpanded: false);

        if (!string.IsNullOrEmpty(field.RelationProgId) || field.RelationFieldMappings is { Count: > 0 })
            node.AddChild(BuildRelationGroup(field));

        if (!string.IsNullOrEmpty(field.LookupProgId) || field.LookupFieldMappings is { Count: > 0 })
            node.AddChild(BuildLookupGroup(field));

        if (field.ListItems is { Count: > 0 } || !string.IsNullOrEmpty(field.LangEnumName))
            node.AddChild(BuildListItemsGroup(field));

        return node;
    }

    public static SettingsTreeNode BuildRelationGroup(FormField field)
    {
        var group = Make(FormSchemaKinds.RelationGroup, "IconLink", field, isExpanded: false);
        if (field.RelationFieldMappings is { } mappings)
            foreach (var mapping in mappings)
                group.AddChild(BuildMapping(mapping));
        return group;
    }

    public static SettingsTreeNode BuildLookupGroup(FormField field)
    {
        var group = Make(FormSchemaKinds.LookupGroup, "IconLookup", field, isExpanded: false);
        if (field.LookupFieldMappings is { } mappings)
            foreach (var mapping in mappings)
                group.AddChild(BuildMapping(mapping));
        return group;
    }

    public static SettingsTreeNode BuildListItemsGroup(FormField field)
    {
        var group = Make(FormSchemaKinds.ListItemsGroup, "IconList", field, isExpanded: false);
        if (field.ListItems is { } items)
            foreach (var item in items)
                group.AddChild(BuildListItem(item));
        return group;
    }

    public static SettingsTreeNode BuildMapping(FieldMapping mapping) =>
        Make(FormSchemaKinds.Mapping, "IconArrowRight", mapping, isExpanded: false);

    public static SettingsTreeNode BuildListItem(Bee.Definition.Collections.ListItem item) =>
        Make(FormSchemaKinds.ListItem, "IconDot", item, isExpanded: false);

    private static SettingsTreeNode Make(string kind, string icon, object payload, bool isExpanded) =>
        SettingsTreeNode.Create(icon, kind, payload, Refresh, isExpanded);

    /// <summary>
    /// Shared <see cref="SettingsTreeNode.Refresher"/> for every FormSchema-tree
    /// node. Delegates header/detail formatting to
    /// <see cref="FormSchemaNodeDisplay.For"/> so all kinds share one dispatch.
    /// </summary>
    private static void Refresh(SettingsTreeNode node)
    {
        var (header, detail) = FormSchemaNodeDisplay.For(node.Kind, node.Payload);
        node.Header = header;
        node.Detail = detail;
    }

    private static string FieldIcon(FormField field) => field.Type switch
    {
        FieldType.DbField => "IconColumn",
        FieldType.RelationField => "IconLink",
        _ => "IconText",
    };
}
