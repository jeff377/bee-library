using System.Globalization;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Db.Storage
{
    /// <summary>
    /// Database-backed <see cref="IDefineStorage"/>: stores each definition as one XML-serialized
    /// row in the single <c>st_define</c> table (keyed by <c>define_type</c> + <c>customize_id</c>
    /// + <c>define_key</c>). Each <c>SaveX</c> performs, in one transaction, the UPSERT plus an
    /// <see cref="ICacheNotifyService.Touch"/> on the matching cache key, so other processes/nodes
    /// observe the change via the notification table and evict the corresponding cache.
    /// </summary>
    /// <remarks>
    /// Covers the six DB-storable definition types (<c>DbCategorySettings</c>, <c>TableSchema</c>,
    /// <c>FormSchema</c>, <c>FormLayout</c>, <c>Language</c>; <c>ProgramSettings</c> arrives with a
    /// later phase). <c>SystemSettings</c> / <c>DatabaseSettings</c> remain file-based (startup
    /// bootstrap) and never reach this storage.
    /// <para>
    /// <c>define_type</c> is the cached type's name (<c>typeof(T).Name</c>), so it equals the cache
    /// group used by the cache container's convention-based eviction dispatch — the bump key
    /// <c>"{typeof(T).Name}:{defineKey}"</c> routes straight to the right cache. Base-layer rows use
    /// <c>customize_id = "*"</c> and singleton types use <c>define_key = "*"</c> (non-empty sentinels,
    /// because Oracle treats <c>''</c> as <c>NULL</c> and primary-key columns cannot be NULL).
    /// </para>
    /// </remarks>
    public sealed class DbDefineStorage : IDefineStorage, ICustomizeDefineReader
    {
        /// <summary>The database identifier hosting <c>st_define</c> (and <c>st_cache_notify</c>).</summary>
        public const string DefineDatabaseId = "common";

        private const string BaseCustomizeId = "*";
        private const string SingletonKey = "*";

        private const string TableName = "st_define";
        private const string TypeColumn = "define_type";
        private const string CustomizeColumn = "customize_id";
        private const string KeyColumn = "define_key";
        private const string ContentColumn = "content";
        private const string UpdateTimeColumn = "sys_update_time";

        private readonly IServiceProvider? _serviceProvider;
        private IDbConnectionManager? _connectionManager;
        private ICacheNotifyService? _cacheNotify;
        private readonly string _databaseId;

        /// <summary>
        /// Initializes a new <see cref="DbDefineStorage"/> with explicit dependencies (used by tests
        /// and direct construction).
        /// </summary>
        /// <param name="connectionManager">Supplies connections and the dialect for the define database.</param>
        /// <param name="cacheNotify">Bumps the notification row in the same transaction as each save.</param>
        /// <param name="databaseId">
        /// The database hosting <c>st_define</c>; defaults to <see cref="DefineDatabaseId"/>
        /// (<c>common</c>). Tests pass a dialect-specific id (e.g. <c>common_postgresql</c>).
        /// </param>
        public DbDefineStorage(IDbConnectionManager connectionManager, ICacheNotifyService cacheNotify, string databaseId = DefineDatabaseId)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _cacheNotify = cacheNotify ?? throw new ArgumentNullException(nameof(cacheNotify));
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            _databaseId = databaseId;
        }

        /// <summary>
        /// Initializes a new <see cref="DbDefineStorage"/> that resolves its dependencies lazily from
        /// <paramref name="serviceProvider"/> on first use. This is the constructor used when the
        /// framework activates DB storage via DI: resolving <see cref="IDbConnectionManager"/> at
        /// construction would form a cycle (connection manager → database settings → define access →
        /// define storage), so resolution is deferred until the first read/write, by which time the
        /// object graph is fully built.
        /// </summary>
        /// <param name="serviceProvider">The application service provider.</param>
        public DbDefineStorage(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _databaseId = DefineDatabaseId;
        }

        private IDbConnectionManager ConnectionManager => _connectionManager ??= Resolve<IDbConnectionManager>();

        private ICacheNotifyService CacheNotify => _cacheNotify ??= Resolve<ICacheNotifyService>();

        private T Resolve<T>()
            => (T)(_serviceProvider!.GetService(typeof(T))
                ?? throw new InvalidOperationException($"Required service is not available: {typeof(T).Name}."));

        #region IDefineStorage

        /// <inheritdoc/>
        public DbCategorySettings? GetDbCategorySettings()
            => ReadRequired<DbCategorySettings>(BaseCustomizeId, SingletonKey);

        /// <inheritdoc/>
        public void SaveDbCategorySettings(DbCategorySettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            Write(settings, SingletonKey);
        }

        /// <inheritdoc/>
        public ProgramSettings? GetProgramSettings()
            => ReadRequired<ProgramSettings>(BaseCustomizeId, SingletonKey);

        /// <inheritdoc/>
        public void SaveProgramSettings(ProgramSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            Write(settings, SingletonKey);
        }

        /// <inheritdoc/>
        public TableSchema? GetTableSchema(string categoryId, string tableName)
            => ReadRequired<TableSchema>(BaseCustomizeId, TableSchemaKey(categoryId, tableName));

        /// <inheritdoc/>
        public void SaveTableSchema(string categoryId, TableSchema tableSchema)
        {
            ArgumentNullException.ThrowIfNull(tableSchema);
            Write(tableSchema, TableSchemaKey(categoryId, tableSchema.TableName));
        }

        /// <inheritdoc/>
        public FormSchema? GetFormSchema(string progId)
            => ReadRequired<FormSchema>(BaseCustomizeId, progId);

        /// <inheritdoc/>
        public void SaveFormSchema(FormSchema formSchema)
        {
            ArgumentNullException.ThrowIfNull(formSchema);
            Write(formSchema, formSchema.ProgId);
        }

        /// <inheritdoc/>
        public FormLayout? GetFormLayout(string layoutId)
            => ReadRequired<FormLayout>(BaseCustomizeId, layoutId);

        /// <inheritdoc/>
        public void SaveFormLayout(FormLayout formLayout)
        {
            ArgumentNullException.ThrowIfNull(formLayout);
            Write(formLayout, formLayout.LayoutId);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Like the file storage, a missing language resource returns <c>null</c> (not an error) —
        /// untranslated namespaces are a normal scenario.
        /// </remarks>
        public LanguageResource? GetLanguage(string lang, string ns)
            => ReadOptional<LanguageResource>(BaseCustomizeId, LanguageKey(lang, ns));

        /// <inheritdoc/>
        public void SaveLanguage(LanguageResource resource)
        {
            ArgumentNullException.ThrowIfNull(resource);
            Write(resource, LanguageKey(resource.Lang, resource.Namespace));
        }

        #endregion

        #region ICustomizeDefineReader

        /// <inheritdoc/>
        public LanguageResource? GetCustomizeLanguage(string customizeId, string lang, string ns)
            => ReadOptional<LanguageResource>(customizeId, LanguageKey(lang, ns));

        /// <inheritdoc/>
        public ProgramSettings? GetCustomizeProgramSettings(string customizeId)
            => ReadOptional<ProgramSettings>(customizeId, SingletonKey);

        /// <inheritdoc/>
        public FormLayout? GetCustomizeFormLayout(string customizeId, string layoutId)
            => ReadOptional<FormLayout>(customizeId, layoutId);

        #endregion

        // Composite define_key must equal the cache's Remove key so eviction routing hits the entry:
        // TableSchemaCache / LanguageResourceCache key on "{a}.{b}" (dot separator).
        private static string TableSchemaKey(string categoryId, string tableName) => $"{categoryId}.{tableName}";

        private static string LanguageKey(string lang, string ns) => $"{lang}.{ns}";

        /// <summary>
        /// Reads a definition that must exist; throws when the row is absent (mirrors the file
        /// storage, where a missing definition file signals a bug).
        /// </summary>
        private T ReadRequired<T>(string customizeId, string defineKey) where T : class
        {
            var xml = ReadContent(typeof(T).Name, customizeId, defineKey);
            if (xml == null)
                throw new InvalidOperationException($"Definition not found: {typeof(T).Name} / {customizeId} / {defineKey}.");
            return XmlCodec.Deserialize<T>(xml)
                ?? throw new InvalidOperationException($"Failed to deserialize definition: {typeof(T).Name} / {customizeId} / {defineKey}.");
        }

        /// <summary>
        /// Reads a definition that may be absent; returns <c>null</c> when the row does not exist.
        /// </summary>
        private T? ReadOptional<T>(string customizeId, string defineKey) where T : class
        {
            var xml = ReadContent(typeof(T).Name, customizeId, defineKey);
            return xml == null ? null : XmlCodec.Deserialize<T>(xml);
        }

        private string? ReadContent(string defineType, string customizeId, string defineKey)
        {
            var dbAccess = new DbAccess(_databaseId, ConnectionManager);
            var databaseType = dbAccess.DatabaseType;

            string tbl = databaseType.QuoteIdentifier(TableName);
            string type = databaseType.QuoteIdentifier(TypeColumn);
            string cust = databaseType.QuoteIdentifier(CustomizeColumn);
            string key = databaseType.QuoteIdentifier(KeyColumn);
            string content = databaseType.QuoteIdentifier(ContentColumn);

            var result = dbAccess.ExecuteScalar(
                $"SELECT {content} FROM {tbl} WHERE {type} = {{0}} AND {cust} = {{1}} AND {key} = {{2}}",
                defineType, customizeId, defineKey);

            if (result is null || result is DBNull) return null;
            return Convert.ToString(result, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// UPSERTs the base-layer row and bumps the matching cache key in one transaction, so the
        /// notification and the data change commit together.
        /// </summary>
        private void Write<T>(T value, string defineKey) where T : class
        {
            string defineType = typeof(T).Name;
            string xml = XmlCodec.Serialize(value);

            var connInfo = ConnectionManager.GetConnectionInfo(_databaseId);
            var databaseType = connInfo.DatabaseType;

            using var connection = ConnectionManager.CreateConnection(_databaseId);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var dbAccess = new DbAccess(connection, databaseType);
            dbAccess.Execute(BuildUpsertSpec(databaseType, defineType, defineKey, xml), transaction);

            CacheNotify.Touch($"{defineType}:{defineKey}", transaction, databaseType);

            transaction.Commit();
        }

        /// <summary>
        /// Builds the dialect-specific UPSERT for <c>st_define</c>. Params: {0}=define_type,
        /// {1}=customize_id (base sentinel), {2}=define_key, {3}=content (XML).
        /// </summary>
        private static DbCommandSpec BuildUpsertSpec(DatabaseType databaseType, string defineType, string defineKey, string content)
        {
            string now = DbDialectRegistry.Get(databaseType).GetDefaultValueExpression(FieldDbType.DateTime);

            string tbl = databaseType.QuoteIdentifier(TableName);
            string type = databaseType.QuoteIdentifier(TypeColumn);
            string cust = databaseType.QuoteIdentifier(CustomizeColumn);
            string key = databaseType.QuoteIdentifier(KeyColumn);
            string cnt = databaseType.QuoteIdentifier(ContentColumn);
            string upd = databaseType.QuoteIdentifier(UpdateTimeColumn);

            string commandText = databaseType switch
            {
                // PostgreSQL / SQLite: named params allow reusing {3} in the DO UPDATE SET clause.
                DatabaseType.PostgreSQL or DatabaseType.SQLite =>
                    $"INSERT INTO {tbl} ({type}, {cust}, {key}, {cnt}, {upd}) VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {now}) " +
                    $"ON CONFLICT ({type}, {cust}, {key}) DO UPDATE SET {cnt} = {{3}}, {upd} = {now}",

                DatabaseType.MySQL =>
                    $"INSERT INTO {tbl} ({type}, {cust}, {key}, {cnt}, {upd}) VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {now}) " +
                    $"ON DUPLICATE KEY UPDATE {cnt} = {{3}}, {upd} = {now}",

                // SQL Server: carry content through the USING source so each param appears once.
                DatabaseType.SQLServer =>
                    $"MERGE {tbl} WITH (HOLDLOCK) AS t USING (VALUES ({{0}}, {{1}}, {{2}}, {{3}})) AS s ({type}, {cust}, {key}, {cnt}) " +
                    $"ON t.{type} = s.{type} AND t.{cust} = s.{cust} AND t.{key} = s.{key} " +
                    $"WHEN MATCHED THEN UPDATE SET t.{cnt} = s.{cnt}, t.{upd} = {now} " +
                    $"WHEN NOT MATCHED THEN INSERT ({type}, {cust}, {key}, {cnt}, {upd}) VALUES (s.{type}, s.{cust}, s.{key}, s.{cnt}, {now});",

                // Oracle: positional binding by default — carry every param through USING so none is
                // referenced twice (reusing a placeholder would expect an extra positional bind).
                DatabaseType.Oracle =>
                    $"MERGE INTO {tbl} t USING (SELECT {{0}} AS {type}, {{1}} AS {cust}, {{2}} AS {key}, {{3}} AS {cnt} FROM dual) s " +
                    $"ON (t.{type} = s.{type} AND t.{cust} = s.{cust} AND t.{key} = s.{key}) " +
                    $"WHEN MATCHED THEN UPDATE SET t.{cnt} = s.{cnt}, t.{upd} = {now} " +
                    $"WHEN NOT MATCHED THEN INSERT ({type}, {cust}, {key}, {cnt}, {upd}) VALUES (s.{type}, s.{cust}, s.{key}, s.{cnt}, {now})",

                _ => throw new NotSupportedException($"Define-storage upsert is not defined for {databaseType}.")
            };

            return new DbCommandSpec(DbCommandKind.NonQuery, commandText, defineType, BaseCustomizeId, defineKey, content);
        }
    }
}
