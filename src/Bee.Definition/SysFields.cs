namespace Bee.Definition
{
    /// <summary>
    /// System field name constants.
    /// </summary>
    public static class SysFields
    {
        // ---- System row identification ----
        /// <summary>
        /// Sequential number, auto-incremented.
        /// </summary>
        public const string No = "sys_no";
        /// <summary>
        /// Unique row identifier.
        /// </summary>
        public const string RowId = "sys_rowid";
        /// <summary>
        /// Master record unique identifier.
        /// A foreign key in the detail table that references the corresponding row in the master table via <see cref="RowId"/>.
        /// </summary>
        public const string MasterRowId = "sys_master_rowid";

        // ---- Basic data fields ----
        /// <summary>
        /// Record number or document number.
        /// </summary>
        public const string Id = "sys_id";
        /// <summary>
        /// Name field, e.g., employee name or department name.
        /// </summary>
        public const string Name = "sys_name";

        // ---- Operator information ----
        /// <summary>
        /// Unique identifier of the creator (foreign key referencing sys_rowid of the user table).
        /// </summary>
        public const string InsertUserRowId = "sys_insert_user_rowid";
        /// <summary>
        /// Unique identifier of the last updater (foreign key referencing sys_rowid of the user table).
        /// </summary>
        public const string UpdateUserRowId = "sys_update_user_rowid";

        // ---- Lifecycle ----
        /// <summary>
        /// Record creation timestamp.
        /// </summary>
        public const string InsertTime = "sys_insert_time";
        /// <summary>
        /// Record last update timestamp.
        /// </summary>
        public const string UpdateTime = "sys_update_time";
        /// <summary>
        /// Record effective date.
        /// The record becomes valid starting from this date (inclusive).
        /// To query active records, use the condition:
        /// <c>CURRENT_DATE >= sys_valid_date</c>
        /// </summary>
        public const string ValidDate = "sys_valid_date";
        /// <summary>
        /// Record expiry date.
        /// The record is no longer valid on or after this date; NULL means still valid (exclusive of the expiry day).
        /// To query active records, use the condition:
        /// <c>sys_invalid_date IS NULL OR CURRENT_DATE &lt; sys_invalid_date</c>
        /// </summary>
        public const string InvalidDate = "sys_invalid_date";
    }
}
