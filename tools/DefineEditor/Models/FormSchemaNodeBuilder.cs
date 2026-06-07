using Bee.Definition.Database;
using Bee.Definition.Forms;

namespace Bee.DefineEditor.Models;

/// <summary>
/// Builds <see cref="FormSchemaTreeNode"/> instances from Bee.Definition payloads.
/// Each builder sets payload + icon + initial expansion + delegates header/detail
/// formatting to <see cref="FormSchemaNodeDisplay"/>.
/// </summary>
internal static class FormSchemaNodeBuilder
{
    public static FormSchemaTreeNode BuildSchema(FormSchema schema)
    {
        var node = Make(FormSchemaNodeKind.Schema, "DefFormSchema", schema, isExpanded: true);
        if (schema.Tables is { } tables)
            foreach (var table in tables)
                node.AddChild(BuildTable(table));
        return node;
    }

    public static FormSchemaTreeNode BuildTable(FormTable table)
    {
        var node = Make(FormSchemaNodeKind.Table, "IconTable", table, isExpanded: true);
        if (table.Fields is { } fields)
            foreach (var field in fields)
                node.AddChild(BuildField(field));
        return node;
    }

    public static FormSchemaTreeNode BuildField(FormField field)
    {
        var node = Make(FormSchemaNodeKind.Field, FieldIcon(field), field, isExpanded: false);

        if (!string.IsNullOrEmpty(field.RelationProgId) || field.RelationFieldMappings is { Count: > 0 })
            node.AddChild(BuildRelationGroup(field));

        if (!string.IsNullOrEmpty(field.LookupProgId) || field.LookupFieldMappings is { Count: > 0 })
            node.AddChild(BuildLookupGroup(field));

        if (field.ListItems is { Count: > 0 } || !string.IsNullOrEmpty(field.LangEnumName))
            node.AddChild(BuildListItemsGroup(field));

        return node;
    }

    public static FormSchemaTreeNode BuildRelationGroup(FormField field)
    {
        var group = Make(FormSchemaNodeKind.RelationGroup, "IconLink", field, isExpanded: false);
        if (field.RelationFieldMappings is { } mappings)
            foreach (var mapping in mappings)
                group.AddChild(BuildMapping(mapping));
        return group;
    }

    public static FormSchemaTreeNode BuildLookupGroup(FormField field)
    {
        var group = Make(FormSchemaNodeKind.LookupGroup, "IconLookup", field, isExpanded: false);
        if (field.LookupFieldMappings is { } mappings)
            foreach (var mapping in mappings)
                group.AddChild(BuildMapping(mapping));
        return group;
    }

    public static FormSchemaTreeNode BuildListItemsGroup(FormField field)
    {
        var group = Make(FormSchemaNodeKind.ListItemsGroup, "IconList", field, isExpanded: false);
        if (field.ListItems is { } items)
            foreach (var item in items)
                group.AddChild(BuildListItem(item));
        return group;
    }

    public static FormSchemaTreeNode BuildMapping(FieldMapping mapping) =>
        Make(FormSchemaNodeKind.Mapping, "IconArrowRight", mapping, isExpanded: false);

    public static FormSchemaTreeNode BuildListItem(Bee.Definition.Collections.ListItem item) =>
        Make(FormSchemaNodeKind.ListItem, "IconDot", item, isExpanded: false);

    private static FormSchemaTreeNode Make(FormSchemaNodeKind kind, string icon, object payload, bool isExpanded)
    {
        var node = new FormSchemaTreeNode
        {
            Kind = kind,
            Icon = icon,
            Payload = payload,
            IsExpanded = isExpanded,
        };
        node.RefreshDisplay();
        return node;
    }

    private static string FieldIcon(FormField field) => field.Type switch
    {
        FieldType.DbField => "IconColumn",
        FieldType.RelationField => "IconLink",
        _ => "IconText",
    };
}
