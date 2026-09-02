using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Process-wide test DB infrastructure: registers ADO.NET <c>DbProviderFactory</c>
    /// + framework <c>IDbDialectFactory</c> per <see cref="DatabaseType"/>, seeds one
    /// <see cref="DatabaseServer"/> plus one <see cref="DatabaseItem"/> per category
    /// declared in <c>DbCategorySettings.xml</c> when the corresponding
    /// <c>BEE_TEST_CONNSTR_*</c> env var is set, and (on opt-in) creates / upgrades the
    /// physical schema for every <c>(category, table)</c> in <c>DbCategorySettings.xml</c>
    /// plus inserts a seed user. All operations are idempotent and guarded both by a
    /// process-wide lock (concurrent fixture ctors) and by a machine-wide
    /// <see cref="CrossProcessLock"/> (test assemblies run as parallel processes against the
    /// same physical database, so every check-then-write here is otherwise a race).
    /// </summary>
    /// <remarks>
    /// Each <see cref="DatabaseItem"/> carries <c>DbName = CategoryId</c> so the server's
    /// <c>{@DbName}</c> placeholder substitution produces a per-category physical database
    /// (e.g. SQL Server <c>common</c> + <c>company</c>). Oracle is the only exception:
    /// per-category DbName is left empty and all 5 tables live in the same testuser schema.
    /// SQLite uses <c>{@DbName}</c> as the in-memory shared-cache filename, producing one
    /// independent in-memory database per category.
    /// </remarks>
    public static partial class SharedDatabaseState
    {
        private static readonly Lock s_registerLock = new();
        private static bool _registered;

        private static readonly Lock s_schemaLock = new();
        private static bool _schemaInitialised;

        // The resource this guards is the database container shared by every test process on
        // the machine, not this working copy, so the name is deliberately not path-derived:
        // two clones running their suites at the same time must contend on the same file.
        private const string SetupLockFileName = "bee-tests-shared-db-setup.lock";

        // Generous enough to cover a full five-database build on a cold container; on expiry
        // the holder is presumed stuck and setup proceeds unlocked (see CrossProcessLock).
        private static readonly TimeSpan s_setupLockTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Registers DB providers / dialect factories and seeds <see cref="DatabaseServer"/>
        /// + per-category <see cref="DatabaseItem"/> values for every <see cref="DatabaseType"/>
        /// whose connection string env var is set. Idempotent across the process.
        /// </summary>
        /// <param name="bootstrapAccess">
        /// A <c>CacheDefineAccess</c> backed by the same <c>CacheContainer</c> the rest
        /// of the framework will read from; new items are added to its <c>DatabaseSettings</c>.
        /// </param>
        public static void EnsureRegistered(IDefineAccess bootstrapAccess)
        {
            ArgumentNullException.ThrowIfNull(bootstrapAccess);
            lock (s_registerLock)
            {
                if (_registered) return;

                var categoryIds = GetCategoryIds(bootstrapAccess);
                RegisterSqlServer(bootstrapAccess, categoryIds);
                RegisterPostgreSql(bootstrapAccess, categoryIds);
                RegisterSqlite(bootstrapAccess, categoryIds);
                RegisterMySql(bootstrapAccess, categoryIds);
                RegisterOracle(bootstrapAccess, categoryIds);
                EnsureFallbackCommonDatabaseItem(bootstrapAccess);

                _registered = true;
            }
        }

        /// <summary>
        /// Re-applies the <see cref="DatabaseServer"/> / <see cref="DatabaseItem"/> entries that
        /// <see cref="EnsureRegistered"/> registered, for callers holding a
        /// <see cref="IDefineAccess"/> whose <c>DatabaseSettings</c> may have been reloaded since.
        /// </summary>
        /// <remarks>
        /// Registration happens once per process, but the settings object it writes to is a shared
        /// cache slot: any test invalidating that slot (<c>SaveDatabaseSettings</c> against its own
        /// temp directory does it) sends the next reader back to <c>DatabaseSettings.xml</c>, where
        /// the test databases do not exist. Every fixture therefore re-applies before use instead
        /// of trusting that nobody else has touched the slot.
        /// </remarks>
        /// <param name="access">The define access whose <c>DatabaseSettings</c> to top up.</param>
        public static void EnsureDatabaseSettingsApplied(IDefineAccess access)
        {
            ArgumentNullException.ThrowIfNull(access);
            lock (s_registerLock)
            {
                var dbSettings = access.GetDatabaseSettings();
                foreach (var server in s_registeredServers)
                {
                    if (!dbSettings.Servers!.Contains(server.Id)) dbSettings.Servers.Add(NewServer(server));
                }
                foreach (var item in s_registeredItems)
                {
                    if (!dbSettings.Items!.Contains(item.Id)) dbSettings.Items.Add(NewItem(item));
                }
            }
        }

        /// <summary>
        /// Verifies connectivity, creates / upgrades schemas for every
        /// <c>(category, table)</c> declared in <c>DbCategorySettings.xml</c>, and inserts
        /// seed data for every registered database. Skips any DB whose env var is unset or
        /// whose connection fails; a database that answers but ends up without its seed user
        /// throws rather than leaving the failure to be rediscovered downstream. Idempotent
        /// across the process.
        /// </summary>
        /// <param name="access">An <see cref="IDefineAccess"/> resolving the same
        /// <c>DatabaseSettings</c> that <see cref="EnsureRegistered"/> populated.</param>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public static void EnsureSchemaAndSeed(IDefineAccess access, IDbConnectionManager connectionManager)
        {
            ArgumentNullException.ThrowIfNull(access);
            ArgumentNullException.ThrowIfNull(connectionManager);
            lock (s_schemaLock)
            {
                if (_schemaInitialised) return;

                // `s_schemaLock` only makes this once-per-process; the whole setup is also
                // once-per-machine because test assemblies run as parallel processes against
                // one physical database and every step here is a check-then-write. Serialising
                // the whole block — not the individual statements — is what lets a loser
                // observe the winner's state as finished rather than half-built.
                using (CrossProcessLock.Acquire(SetupLockFileName, s_setupLockTimeout))
                {
                    EnsureDatabase(DatabaseType.SQLServer, access, connectionManager);
                    EnsureDatabase(DatabaseType.PostgreSQL, access, connectionManager);
                    EnsureDatabase(DatabaseType.SQLite, access, connectionManager);
                    EnsureDatabase(DatabaseType.MySQL, access, connectionManager);
                    EnsureDatabase(DatabaseType.Oracle, access, connectionManager);
                }

                _schemaInitialised = true;
            }
        }
    }
}
