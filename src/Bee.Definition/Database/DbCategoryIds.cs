namespace Bee.Definition.Database
{
    /// <summary>
    /// Built-in database category identifiers used by the framework.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These constants serve two roles:
    /// </para>
    /// <list type="bullet">
    /// <item><b>CategoryId</b> values for logical classification on
    /// <see cref="Bee.Definition.Settings.DatabaseItem.CategoryId"/> and <see cref="Bee.Definition.Forms.FormSchema.CategoryId"/>.</item>
    /// <item><b>DatabaseId</b> conventions for framework system routing
    /// (e.g., <c>SessionRepository</c> uses <c>Common</c> as the literal
    /// <see cref="Bee.Definition.Settings.DatabaseItem.Id"/> for the shared system database).</item>
    /// </list>
    /// <para>
    /// In multi-tenant or time-archived deployments, the physical
    /// <see cref="Bee.Definition.Settings.DatabaseItem.Id"/> may diverge from the CategoryId (e.g.,
    /// <c>company001</c>, <c>log2025</c>), but the <see cref="Bee.Definition.Settings.DatabaseItem.CategoryId"/>
    /// remains one of these constants. For the <see cref="Common"/> category,
    /// the framework requires Id == CategoryId == "common" (enforced at startup
    /// by <c>services.AddBeeFramework</c>).
    /// </para>
    /// </remarks>
    public static class DbCategoryIds
    {
        /// <summary>
        /// Common database — shared system tables (e.g., st_user, st_session).
        /// </summary>
        public const string Common = "common";

        /// <summary>
        /// Company database — business data, isolated per company.
        /// </summary>
        public const string Company = "company";

        /// <summary>
        /// Log database — audit / operation logs.
        /// </summary>
        public const string Log = "log";
    }
}
