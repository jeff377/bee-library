using System;

namespace Bee.Definition
{
    #region Constants

    /// <summary>
    /// Defines default type name constants for commonly used backend implementations.
    /// Can be used for type specification in the SystemSettings.xml configuration file or as default fallback values.
    /// </summary>
    public static class BackendDefaultTypes
    {
        // ---------------- Providers ----------------
        /// <summary>
        /// Default API encryption key provider type.
        /// </summary>
        public const string ApiEncryptionKeyProvider = "Bee.Business.Provider.DynamicApiEncryptionKeyProvider, Bee.Business";
        /// <summary>
        /// Default AccessToken validation provider, used to validate the validity of AccessTokens.
        /// </summary>
        public const string AccessTokenValidationProvider = "Bee.Business.Validator.AccessTokenValidationProvider, Bee.Business";
        /// <summary>
        /// Default business object provider type, used for dynamically creating BusinessObjects.
        /// </summary>
        public const string BusinessObjectProvider = "Bee.Business.BusinessObjects.BusinessObjectProvider, Bee.Business";

        // ---------------- Cache ----------------
        /// <summary>
        /// Default cache provider type.
        /// </summary>
        public const string CacheProvider = "Bee.ObjectCaching.Providers.MemoryCacheProvider, Bee.ObjectCaching";
        /// <summary>
        /// Default cache data source provider type.
        /// </summary>
        public const string CacheDataSourceProvider = "Bee.Business.Provider.CacheDataSourceProvider, Bee.Business";

        // ---------------- Define ----------------
        /// <summary>
        /// Default define storage type.
        /// </summary>
        public const string DefineStorage = "Bee.Definition.Storage.FileDefineStorage, Bee.Definition";
        /// <summary>
        /// Default define access type.
        /// </summary>
        public const string DefineAccess = "Bee.ObjectCaching.LocalDefineAccess, Bee.ObjectCaching";

        // ---------------- Services ----------------
        /// <summary>
        /// Default session info service type.
        /// </summary>
        public const string SessionInfoService = "Bee.ObjectCaching.Services.SessionInfoService, Bee.ObjectCaching";
        /// <summary>
        /// Default unified access service type for commonly used enterprise business objects.
        /// </summary>
        public const string EnterpriseObjectService = "Bee.ObjectCaching.Services.EnterpriseObjectService, Bee.ObjectCaching";

        // ---------------- Repository ----------------
        /// <summary>
        /// Default system-level repository provider type.
        /// </summary>
        public const string SystemRepositoryProvider = "Bee.Repository.Provider.SystemRepositoryProvider, Bee.Repository";
        /// <summary>
        /// Default form-level repository provider type.
        /// </summary>
        public const string FormRepositoryProvider = "Bee.Repository.Provider.FormRepositoryProvider, Bee.Repository";
    }

    /// <summary>
    /// Action name constants for the SystemObject.
    /// </summary>
    public class SystemActions
    {
        /// <summary>
        /// Ping method — tests whether the API service is available. Data encoding is not enabled for this method.
        /// </summary>
        public const string Ping = "Ping";
        /// <summary>
        /// Gets common configuration parameters and environment settings. Data encoding is not enabled for this method.
        /// </summary>
        public const string GetCommonConfiguration = "GetCommonConfiguration";
        /// <summary>
        /// Performs the login operation. Encryption is not enabled for this method.
        /// </summary>
        public const string Login = "Login";
        /// <summary>
        /// Creates a session. Data encoding is not enabled for this method.
        /// </summary>
        public const string CreateSession = "CreateSession";
        /// <summary>
        /// Gets definition data.
        /// </summary>
        public const string GetDefine = "GetDefine";
        /// <summary>
        /// Gets definition data (local only).
        /// </summary>
        public const string GetLocalDefine = "GetLocalDefine";
        /// <summary>
        /// Saves definition data.
        /// </summary>
        public const string SaveDefine = "SaveDefine";
        /// <summary>
        /// Saves definition data (local only).
        /// </summary>
        public const string SaveLocalDefine = "SaveLocalDefine";
        /// <summary>
        /// Executes a custom function.
        /// </summary>
        public const string ExecFunc = "ExecFunc";
        /// <summary>
        /// Executes a custom function with anonymous access.
        /// </summary>
        public const string ExecFuncAnonymous = "ExecFuncAnonymous";
        /// <summary>
        /// Executes a custom function — local calls only.
        /// </summary>
        public const string ExecFuncLocal = "ExecFuncLocal";
    }

    /// <summary>
    /// FuncID constants used by the framework.
    /// </summary>
    public class SysFuncIDs
    {
        /// <summary>
        /// Hello test method.
        /// </summary>
        public const string Hello = "Hello";
        /// <summary>
        /// Upgrades the table schema.
        /// </summary>
        public const string UpgradeTableSchema = "UpgradeTableSchema";
        /// <summary>
        /// Tests the database connection.
        /// </summary>
        public const string TestConnection = "TestConnection";
    }

    /// <summary>
    /// Program ID constants used by the system.
    /// </summary>
    public class SysProgIds
    {
        /// <summary>
        /// System-level business object.
        /// </summary>
        public const string System = "System";
    }

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

    #endregion

    #region Enumerations

    /// <summary>
    /// The type of a log entry event.
    /// </summary>
    public enum LogEntryType
    {
        /// <summary>
        /// Informational message indicating normal system operation.
        /// </summary>
        Information,
        /// <summary>
        /// Warning message indicating a possible abnormal condition while the system can still continue.
        /// </summary>
        Warning,
        /// <summary>
        /// Error message indicating an exception or failure during system execution.
        /// </summary>
        Error
    }

    /// <summary>
    /// Logging level for abnormal SQL executions.
    /// </summary>
    public enum DbAccessAnomalyLogLevel
    {
        /// <summary>
        /// Disable abnormal logging completely.
        /// </summary>
        None = 0,
        /// <summary>
        /// Log only errors and exceptions.
        /// </summary>
        Error = 1,
        /// <summary>
        /// Log errors, exceptions, and abnormal cases (slow queries, large updates, large result sets).
        /// </summary>
        Warning = 2
    }

    /// <summary>
    /// The source type of the master key.
    /// </summary>
    public enum MasterKeySourceType
    {
        /// <summary>
        /// Load the master key from a file.
        /// </summary>
        File,
        /// <summary>
        /// Load the master key from an environment variable.
        /// </summary>
        Environment
    }

    /// <summary>
    /// Initialization options.
    /// </summary>
    [Flags]
    public enum InitializeOptions
    {
        /// <summary>
        /// Backend initialization.
        /// </summary>
        Backend = 1,
        /// <summary>
        /// Frontend initialization.
        /// </summary>
        Frontend = 2,
        /// <summary>
        /// Website initialization.
        /// </summary>
        Website = 4,
        /// <summary>
        /// Background service initialization.
        /// </summary>
        Background = 8
    }

    /// <summary>
    /// Application type.
    /// </summary>
    public enum ApplicationType
    {
        /// <summary>
        /// Web application.
        /// </summary>
        Website,
        /// <summary>
        /// Windows desktop application.
        /// </summary>
        Windows,
        /// <summary>
        /// Background service application.
        /// </summary>
        BackgroundService
    }

    /// <summary>
    /// API access protection level.
    /// </summary>
    public enum ApiProtectionLevel
    {
        /// <summary>
        /// Public: allows any call without enforced encoding (open to third parties).
        /// </summary>
        Public = 0,
        /// <summary>
        /// Encoded: allows remote calls but requires encoding (serialization and compression).
        /// </summary>
        Encoded = 1,
        /// <summary>
        /// Encrypted: allows remote calls but requires encoding and encryption (serialization, compression, and encryption).
        /// </summary>
        Encrypted = 2,
        /// <summary>
        /// Local only: no encoding validation required; suitable for tools and background services.
        /// </summary>
        LocalOnly = 3
    }

    /// <summary>
    /// API access authentication requirement.
    /// </summary>
    public enum ApiAccessRequirement
    {
        /// <summary>
        /// No login required (anonymous access).
        /// </summary>
        Anonymous = 0,
        /// <summary>
        /// Login required (AccessToken must be validated).
        /// </summary>
        Authenticated = 1
    }

    /// <summary>
    /// Definition data type.
    /// </summary>
    public enum DefineType
    {
        /// <summary>
        /// System settings.
        /// </summary>
        SystemSettings,
        /// <summary>
        /// Database settings.
        /// </summary>
        DatabaseSettings,
        /// <summary>
        /// Database schema settings.
        /// </summary>
        DbSchemaSettings,
        /// <summary>
        /// Program settings list.
        /// </summary>
        ProgramSettings,
        /// <summary>
        /// Table schema.
        /// </summary>
        TableSchema,
        /// <summary>
        /// Form schema definition.
        /// </summary>
        FormSchema,
        /// <summary>
        /// Form layout configuration.
        /// </summary>
        FormLayout
    }

    /// <summary>
    /// Database type.
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// SQL Server.
        /// </summary>
        SQLServer,
        /// <summary>
        /// MySQL.
        /// </summary>
        MySQL,
        /// <summary>
        /// SQLite.
        /// </summary>
        SQLite,
        /// <summary>
        /// Oracle.
        /// </summary>
        Oracle
    }

    /// <summary>
    /// Database schema upgrade action.
    /// </summary>
    public enum DbUpgradeAction
    {
        /// <summary>
        /// Schema is consistent; no upgrade needed.
        /// </summary>
        None,
        /// <summary>
        /// New schema element to be created.
        /// </summary>
        New,
        /// <summary>
        /// Existing schema element to be upgraded.
        /// </summary>
        Upgrade
    }

    /// <summary>
    /// Field type.
    /// </summary>
    public enum FieldType
    {
        /// <summary>
        /// A field that physically exists in the database table.
        /// </summary>
        DbField,
        /// <summary>
        /// A relation field retrieved from another table via a JOIN operation.
        /// </summary>
        RelationField,
        /// <summary>
        /// A virtual field generated by a calculation or expression.
        /// </summary>
        VirtualField
    }

    /// <summary>
    /// Represents a sort direction.
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// Ascending order.
        /// </summary>
        Asc,
        /// <summary>
        /// Descending order.
        /// </summary>
        Desc
    }

    /// <summary>
    /// The kind of a filter node.
    /// </summary>
    public enum FilterNodeKind
    {
        /// <summary>A single-field condition.</summary>
        Condition = 0,
        /// <summary>A condition group.</summary>
        Group = 1
    }

    /// <summary>
    /// Represents a logical operator used to combine groups or query/condition expressions.
    /// </summary>
    /// <remarks>
    /// Describes how multiple conditions are combined (e.g., AND/OR in query conditions).
    /// </remarks>
    public enum LogicalOperator
    {
        /// <summary>
        /// Logical AND.
        /// </summary>
        And = 0,
        /// <summary>
        /// Logical OR.
        /// </summary>
        Or = 1
    }

    /// <summary>
    /// Comparison operator.
    /// Represents the various types of comparison operations available in queries or conditions.
    /// </summary>
    public enum ComparisonOperator
    {
        /// <summary>
        /// Equal to, corresponding to SQL "=".
        /// </summary>
        Equal = 0,
        /// <summary>
        /// Not equal to, corresponding to SQL "&lt;&gt;" or "!=".
        /// </summary>
        NotEqual = 1,
        /// <summary>
        /// Greater than, corresponding to SQL "&gt;".
        /// </summary>
        GreaterThan = 2,
        /// <summary>
        /// Greater than or equal to, corresponding to SQL "&gt;=".
        /// </summary>
        GreaterThanOrEqual = 3,
        /// <summary>
        /// Less than, corresponding to SQL "&lt;".
        /// </summary>
        LessThan = 4,
        /// <summary>
        /// Less than or equal to, corresponding to SQL "&lt;=".
        /// </summary>
        LessThanOrEqual = 5,
        /// <summary>
        /// Pattern matching, corresponding to SQL "LIKE" (the caller must supply appropriate wildcard characters).
        /// </summary>
        Like = 6,
        /// <summary>
        /// Set membership, corresponding to SQL "IN ( ... )".
        /// </summary>
        In = 7,
        /// <summary>
        /// Range check, corresponding to SQL "BETWEEN ... AND ...".
        /// </summary>
        Between = 8,
        /// <summary>
        /// Starts-with match, equivalent to SQL "LIKE 'value%'".
        /// </summary>
        StartsWith = 9,
        /// <summary>
        /// Ends-with match, equivalent to SQL "LIKE '%value'".
        /// </summary>
        EndsWith = 10,
        /// <summary>
        /// Contains match, equivalent to SQL "LIKE '%value%'".
        /// </summary>
        Contains = 11
    }

    #endregion

    #region Layout-related Enumerations

    /// <summary>
    /// Single-record form mode.
    /// </summary>
    public enum SingleFormMode
    {
        /// <summary>
        /// View mode.
        /// </summary>
        View,
        /// <summary>
        /// Add mode.
        /// </summary>
        Add,
        /// <summary>
        /// Edit mode.
        /// </summary>
        Edit
    }

    /// <summary>
    /// Control type.
    /// </summary>
    public enum ControlType
    {
        /// <summary>
        /// Automatically determined.
        /// </summary>
        Auto,
        /// <summary>
        /// Text edit box.
        /// </summary>
        TextEdit,
        /// <summary>
        /// Button edit box.
        /// </summary>
        ButtonEdit,
        /// <summary>
        /// Date input box.
        /// </summary>
        DateEdit,
        /// <summary>
        /// Year-month input box.
        /// </summary>
        YearMonthEdit,
        /// <summary>
        /// Drop-down list.
        /// </summary>
        DropDownEdit,
        /// <summary>
        /// Memo (multi-line text) input box.
        /// </summary>
        MemoEdit,
        /// <summary>
        /// Check box.
        /// </summary>
        CheckEdit
    }

    /// <summary>
    /// Control type for grid columns.
    /// </summary>
    public enum ColumnControlType
    {
        /// <summary>
        /// Automatically determined.
        /// </summary>
        Auto,
        /// <summary>
        /// Text edit box.
        /// </summary>
        TextEdit,
        /// <summary>
        /// Button edit box.
        /// </summary>
        ButtonEdit,
        /// <summary>
        /// Date input box.
        /// </summary>
        DateEdit,
        /// <summary>
        /// Year-month input box.
        /// </summary>
        YearMonthEdit,
        /// <summary>
        /// Drop-down list.
        /// </summary>
        DropDownEdit,
        /// <summary>
        /// Check box.
        /// </summary>
        CheckEdit
    }

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

    #endregion
}
