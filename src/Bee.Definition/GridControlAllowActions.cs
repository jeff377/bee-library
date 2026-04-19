namespace Bee.Definition
{
    /// <summary>
    /// Actions allowed on a grid control.
    /// </summary>
    [Flags]
    public enum GridControlAllowActions
    {
        /// <summary>
        /// No actions allowed.
        /// </summary>
        None = 0,
        /// <summary>
        /// Add action.
        /// </summary>
        Add = 1,
        /// <summary>
        /// Edit action.
        /// </summary>
        Edit = 2,
        /// <summary>
        /// Delete action.
        /// </summary>
        Delete = 4,
        /// <summary>
        /// All actions (Add, Edit, and Delete).
        /// </summary>
        All = Add | Edit | Delete
    }
}
