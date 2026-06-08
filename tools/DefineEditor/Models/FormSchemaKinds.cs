namespace Bee.DefineEditor.Models;

/// <summary>
/// String constants identifying the kind of node in the FormSchema inner tree.
/// Replaces the prior <c>FormSchemaNodeKind</c> enum — once the tree node was
/// consolidated to <see cref="SettingsTreeNode"/> (whose <c>Kind</c> is a
/// <see cref="string"/>), the enum became redundant. Constants live in
/// <c>Models</c> so both the builder/display helpers and the FormSchema
/// view-model can reference them without a Models → ViewModels backlink.
/// </summary>
internal static class FormSchemaKinds
{
    public const string Schema = "Schema";
    public const string Table = "Table";
    public const string Field = "Field";
    public const string RelationGroup = "RelationGroup";
    public const string LookupGroup = "LookupGroup";
    public const string Mapping = "Mapping";
    public const string ListItemsGroup = "ListItemsGroup";
    public const string ListItem = "ListItem";
}
