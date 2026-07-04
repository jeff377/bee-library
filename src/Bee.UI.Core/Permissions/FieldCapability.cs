namespace Bee.UI.Core.Permissions
{
    /// <summary>
    /// The resolved client-side capability of a single form field: whether it should render and,
    /// if it renders, whether it is editable. Produced by
    /// <see cref="IElementCapabilityResolver.ResolveField"/> and combined with the field's layout
    /// state by the consuming UI (visible only when both agree; read-only when either requires it).
    /// </summary>
    /// <param name="Visible">Whether the field should render at all.</param>
    /// <param name="ReadOnly">Whether the field, when visible, is read-only.</param>
    public readonly record struct FieldCapability(bool Visible, bool ReadOnly)
    {
        /// <summary>The unrestricted capability: visible and editable (the opt-out default).</summary>
        public static readonly FieldCapability Allowed = new(true, false);
    }
}
