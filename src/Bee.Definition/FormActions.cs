namespace Bee.Definition
{
    /// <summary>
    /// Action name constants for <c>FormBusinessObject</c> methods.
    /// </summary>
    public static class FormActions
    {
        /// <summary>
        /// Retrieves list-view rows by FormSchema-driven SELECT.
        /// </summary>
        public const string GetList = "GetList";

        /// <summary>
        /// Retrieves lookup candidate rows for picker windows referencing this
        /// form; the projection is server-resolved from <c>FormSchema.LookupFields</c>.
        /// </summary>
        public const string GetLookup = "GetLookup";

        /// <summary>
        /// Returns a blank <c>DataSet</c> skeleton seeded with FormSchema
        /// defaults and a server-issued <c>sys_rowid</c>; step 1 of the
        /// new-and-save flow.
        /// </summary>
        public const string GetNewData = "GetNewData";

        /// <summary>
        /// Loads a single master row (and its details) by <c>RowId</c>; step 1
        /// of the load-and-save flow.
        /// </summary>
        public const string GetData = "GetData";

        /// <summary>
        /// Persists a <c>DataSet</c> by dispatching INSERT / UPDATE / DELETE
        /// based on each row's <c>RowState</c>; step 2 of both the new-and-save
        /// and load-and-save flows.
        /// </summary>
        public const string Save = "Save";

        /// <summary>
        /// Deletes a single master row directly by <c>RowId</c> without first
        /// loading the full <c>DataSet</c>.
        /// </summary>
        public const string Delete = "Delete";
    }
}
