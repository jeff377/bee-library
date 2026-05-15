namespace Bee.Definition
{
    /// <summary>
    /// Identifies which of the three logical databases a bo repo intends to access.
    /// </summary>
    /// <remarks>
    /// Conceptually decoupled from <c>schema.CategoryId</c>: that string is a schema
    /// attribute serialised to XML, while this enum is the runtime access intent used
    /// by <c>IRepositoryDatabaseRouter</c>.
    /// </remarks>
    public enum DbScope
    {
        /// <summary>
        /// Shared cross-company database (e.g. <c>st_user</c>, <c>st_session</c>).
        /// Resolved to the fixed databaseId <c>"common"</c>; does not require a session.
        /// </summary>
        Common,

        /// <summary>
        /// Per-session company database. Resolved by routing through
        /// <c>SessionInfo.CompanyId</c> to <c>CompanyInfo.CompanyDatabaseId</c>.
        /// </summary>
        Company,

        /// <summary>
        /// Shared cross-company log database. Resolved to the fixed databaseId
        /// <c>"log"</c>; does not require a session, allowing pre-EnterCompany
        /// methods (Login, CreateSession, Logout) to write audit log entries.
        /// </summary>
        Log,
    }
}
