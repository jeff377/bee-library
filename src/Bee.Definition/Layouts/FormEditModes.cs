namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Form modes in which editing is allowed. View mode is never editable and is
    /// therefore not part of the flags.
    /// </summary>
    [Flags]
    public enum FormEditModes
    {
        /// <summary>
        /// Editing is not allowed in any mode.
        /// </summary>
        None = 0,
        /// <summary>
        /// Editing is allowed in Add mode.
        /// </summary>
        Add = 1,
        /// <summary>
        /// Editing is allowed in Edit mode.
        /// </summary>
        Edit = 2,
        /// <summary>
        /// Editing is allowed in both Add and Edit modes.
        /// </summary>
        All = Add | Edit
    }
}
