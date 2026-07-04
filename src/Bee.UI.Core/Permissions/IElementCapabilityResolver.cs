using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.UI.Core.Permissions
{
    /// <summary>
    /// Resolves the client-side capability of UI elements (commands, fields, grid actions) from a
    /// per-model permission snapshot. UI-agnostic and pure: every method takes the capability
    /// snapshot as a parameter (typically <see cref="ClientInfo.Capabilities"/>) so the same logic
    /// serves every front end while each UI applies the result its own way.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> snapshot means enforcement is inactive — every method returns the unrestricted
    /// result, leaving the UI unchanged. This is UX degradation only; the backend stays the
    /// authoritative security boundary.
    /// </remarks>
    public interface IElementCapabilityResolver
    {
        /// <summary>
        /// Returns whether a command requiring the given action is permitted. Returns <c>true</c>
        /// (permitted) when the action is <see cref="PermissionAction.None"/> (untagged command),
        /// the snapshot is <c>null</c>, or the form declares no <see cref="FormSchema.PermissionModelId"/>.
        /// Otherwise permitted when the model's mask grants any of the requested action flags.
        /// </summary>
        /// <param name="schema">The form schema whose permission model gates the command.</param>
        /// <param name="action">The action(s) the command requires (may combine flags, e.g. Create|Update).</param>
        /// <param name="capabilities">The per-model capability snapshot, or <c>null</c> when inactive.</param>
        bool Can(FormSchema schema, PermissionAction action, IReadOnlyDictionary<string, PermissionAction>? capabilities);

        /// <summary>
        /// Resolves a field's capability from its <see cref="FormField.SensitiveCategory"/>. A field
        /// with <see cref="SensitiveCategory.None"/> (or an absent field, or a <c>null</c> snapshot)
        /// is unrestricted. A sensitive field is hidden without <c>Read</c> and read-only without
        /// <c>Update</c> on its well-known category model.
        /// </summary>
        /// <param name="schema">The form schema (single source of truth for field metadata).</param>
        /// <param name="fieldName">The field name to resolve.</param>
        /// <param name="tableName">The owning table name; empty resolves to the master table.</param>
        /// <param name="capabilities">The per-model capability snapshot, or <c>null</c> when inactive.</param>
        FieldCapability ResolveField(FormSchema schema, string fieldName, string tableName, IReadOnlyDictionary<string, PermissionAction>? capabilities);

        /// <summary>
        /// Intersects a grid's declared <see cref="LayoutGrid.AllowActions"/> with the form model's
        /// capability: Add requires <c>Create</c>, Edit requires <c>Update</c>, Delete requires
        /// <c>Delete</c>. Returns the declared actions unchanged when the snapshot is <c>null</c> or
        /// the form declares no permission model.
        /// </summary>
        /// <param name="grid">The grid layout declaring the allowed actions.</param>
        /// <param name="schema">The form schema whose permission model gates the grid.</param>
        /// <param name="capabilities">The per-model capability snapshot, or <c>null</c> when inactive.</param>
        GridControlAllowActions ResolveGridActions(LayoutGrid grid, FormSchema schema, IReadOnlyDictionary<string, PermissionAction>? capabilities);
    }
}
