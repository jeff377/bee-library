namespace Bee.Definition.Logging
{
    /// <summary>
    /// The kind of data change recorded in <c>st_log_change</c>, derived from the master row's
    /// state. A details-only change (master unchanged) still counts as an <see cref="Update"/>.
    /// </summary>
    public enum ChangeKind
    {
        /// <summary>A new record was inserted.</summary>
        Insert = 1,

        /// <summary>An existing record was updated (including details-only changes).</summary>
        Update = 2,

        /// <summary>A record was deleted.</summary>
        Delete = 3,
    }
}
