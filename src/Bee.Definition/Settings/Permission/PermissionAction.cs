namespace Bee.Definition.Settings
{
    /// <summary>
    /// The set of permission actions that can be granted on a permission model.
    /// </summary>
    /// <remarks>
    /// CRUD covers the four basic data verbs; <see cref="Print"/> and <see cref="Export"/>
    /// are data-egress actions whose record scope inherits the model's <see cref="Read"/>.
    /// State transitions (Approve / Post / Confirm) are intentionally absent — they belong
    /// to a separate workflow-permission layer, not the action axis.
    /// </remarks>
    [Flags]
    public enum PermissionActions
    {
        /// <summary>No action.</summary>
        None = 0,

        /// <summary>Create a record.</summary>
        Create = 1,

        /// <summary>Read a record.</summary>
        Read = 2,

        /// <summary>Update a record.</summary>
        Update = 4,

        /// <summary>Delete a record.</summary>
        Delete = 8,

        /// <summary>Print formatted document output (a data-egress action).</summary>
        Print = 16,

        /// <summary>Export structured raw data (a data-egress action).</summary>
        Export = 32
    }
}
