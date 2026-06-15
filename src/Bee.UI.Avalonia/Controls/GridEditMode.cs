namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Editing model for <see cref="GridControl"/>. A UI-layer concern by design:
    /// the shared layout definitions stay framework-neutral, so each UI family
    /// decides its own editing model.
    /// </summary>
    public enum GridEditMode
    {
        /// <summary>
        /// In-grid cell editing (the hybrid strategy of ADR-021: text columns use
        /// the DataGrid edit pipeline, popup-based columns swap their editor in).
        /// </summary>
        InCell,

        /// <summary>
        /// The grid stays read-only; rows are edited in a popup edit form built
        /// from the field editors, with commit/cancel semantics.
        /// </summary>
        EditForm,
    }
}
