namespace Bee.DefineEditor.Models;

/// <summary>
/// The role a <see cref="FormSchemaTreeNode"/> plays inside the FormSchema inner tree.
/// </summary>
public enum FormSchemaNodeKind
{
    /// <summary>The root FormSchema (Payload = <c>FormSchema</c>).</summary>
    Schema,
    /// <summary>A FormTable child of the schema (Payload = <c>FormTable</c>).</summary>
    Table,
    /// <summary>A FormField child of a table (Payload = <c>FormField</c>).</summary>
    Field,
    /// <summary>The "Relation" group under a Field (Payload = the owning <c>FormField</c>).</summary>
    RelationGroup,
    /// <summary>The "Lookup" group under a Field (Payload = the owning <c>FormField</c>).</summary>
    LookupGroup,
    /// <summary>A FieldMapping child of a relation/lookup group (Payload = <c>FieldMapping</c>).</summary>
    Mapping,
    /// <summary>The "ListItems" group under a Field (Payload = the owning <c>FormField</c>).</summary>
    ListItemsGroup,
    /// <summary>A ListItem child of the list-items group (Payload = <c>ListItem</c>).</summary>
    ListItem,
}
