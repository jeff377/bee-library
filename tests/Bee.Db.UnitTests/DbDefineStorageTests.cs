using System.ComponentModel;
using System.Globalization;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Db.Storage;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Integration tests for <see cref="DbDefineStorage"/> against a live database per dialect.
    /// Constructs the storage directly with the fixture's connection manager + cache-notify service
    /// (avoiding the DI activation cycle), and exercises round-trip Save/Get plus the same-transaction
    /// notification bump for single-key, composite-key, and optional definition types. Tests skip when
    /// the dialect's <c>BEE_TEST_CONNSTR_*</c> env var is unset.
    /// </summary>
    public class DbDefineStorageTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public DbDefineStorageTests(SharedDbFixture fx) { _fx = fx; }

        private DbDefineStorage NewStorage(DatabaseType databaseType)
        {
            var connectionManager = _fx.GetRequiredService<IDbConnectionManager>();
            var cacheNotify = _fx.GetRequiredService<ICacheNotifyService>();
            var databaseId = TestDbConventions.GetDatabaseId(databaseType);
            return new DbDefineStorage(connectionManager, cacheNotify, databaseId);
        }

        // Reads the notification version for a cache key; -1 when no row exists yet.
        private long CacheVersion(DatabaseType databaseType, string cacheKey)
        {
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(databaseType));
            string tbl = databaseType.QuoteIdentifier("st_cache_notify");
            string keyCol = databaseType.QuoteIdentifier("cache_key");
            string verCol = databaseType.QuoteIdentifier("cache_version");
            var scalar = dbAccess.ExecuteScalar($"SELECT {verCol} FROM {tbl} WHERE {keyCol} = {{0}}", cacheKey);
            if (scalar is null || scalar is DBNull) return -1;
            return Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
        }

        private void RunRoundTrip(DatabaseType databaseType)
        {
            var storage = NewStorage(databaseType);

            // --- FormSchema (single key) round-trip + overwrite bumps the same key ---
            string progId = "RT_" + Guid.NewGuid().ToString("N");
            storage.SaveFormSchema(new FormSchema(progId, "RT Form"));

            var form = storage.GetFormSchema(progId);
            Assert.NotNull(form);
            Assert.Equal(progId, form!.ProgId);
            Assert.Equal("RT Form", form.DisplayName);

            long v1 = CacheVersion(databaseType, $"FormSchema:{progId}");
            Assert.True(v1 >= 1, $"expected bump version >= 1, got {v1}");

            // Overwrite: content updates and the notification version advances.
            storage.SaveFormSchema(new FormSchema(progId, "RT Form v2"));
            Assert.Equal("RT Form v2", storage.GetFormSchema(progId)!.DisplayName);
            Assert.True(CacheVersion(databaseType, $"FormSchema:{progId}") > v1);

            // --- TableSchema (composite key "category.table") ---
            string tableName = "rt_" + Guid.NewGuid().ToString("N");
            storage.SaveTableSchema("common", new TableSchema { TableName = tableName, DisplayName = "RT Table" });

            var table = storage.GetTableSchema("common", tableName);
            Assert.NotNull(table);
            Assert.Equal(tableName, table!.TableName);
            // define_key uses the dot separator the cache keys on, so the bump key aligns.
            Assert.True(CacheVersion(databaseType, $"TableSchema:common.{tableName}") >= 1);

            // --- FormLayout (single key) ---
            string layoutId = "RTL_" + Guid.NewGuid().ToString("N");
            storage.SaveFormLayout(new FormLayout { LayoutId = layoutId });
            Assert.Equal(layoutId, storage.GetFormLayout(layoutId)!.LayoutId);

            // --- Language (optional: missing returns null; then round-trips) ---
            string lang = string.Concat("rt-", Guid.NewGuid().ToString("N").AsSpan(0, 8));
            const string ns = "common";
            Assert.Null(storage.GetLanguage(lang, ns));

            storage.SaveLanguage(new LanguageResource { Lang = lang, Namespace = ns });
            var resource = storage.GetLanguage(lang, ns);
            Assert.NotNull(resource);
            Assert.Equal(lang, resource!.Lang);
            // LanguageResource cache keys on "{lang}.{ns}", so the bump key matches.
            Assert.True(CacheVersion(databaseType, $"LanguageResource:{lang}.{ns}") >= 1);

            // --- ProgramSettings (singleton key "*") round-trip + bump ---
            storage.SaveProgramSettings(new ProgramSettings());
            Assert.NotNull(storage.GetProgramSettings());
            Assert.True(CacheVersion(databaseType, "ProgramSettings:*") >= 1);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：DbDefineStorage 各型別 round-trip + 同 tx bump")]
        public void RoundTrip_SqlServer() => RunRoundTrip(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：DbDefineStorage 各型別 round-trip + 同 tx bump")]
        public void RoundTrip_PostgreSQL() => RunRoundTrip(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：DbDefineStorage 各型別 round-trip + 同 tx bump")]
        public void RoundTrip_MySQL() => RunRoundTrip(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：DbDefineStorage 各型別 round-trip + 同 tx bump")]
        public void RoundTrip_Oracle() => RunRoundTrip(DatabaseType.Oracle);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：缺漏的必要定義 Get 應拋例外")]
        public void GetRequired_Missing_Throws()
        {
            var storage = NewStorage(DatabaseType.SQLServer);
            Assert.Throws<InvalidOperationException>(
                () => storage.GetFormSchema("RT_missing_" + Guid.NewGuid().ToString("N")));
        }

        // IServiceProvider that fails if asked to resolve anything — proves the DI ctor defers
        // resolution (otherwise the DB-storage activation would dead-lock the construction cycle).
        private sealed class ThrowingServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
                => throw new InvalidOperationException("Dependencies must not be resolved at construction.");
        }

        [Fact]
        [DisplayName("以 IServiceProvider 建構不應於建構時解析相依(打破 DI 建構循環)")]
        public void Constructor_ServiceProvider_DefersDependencyResolution()
        {
            var exception = Record.Exception(() => new DbDefineStorage(new ThrowingServiceProvider()));
            Assert.Null(exception);
        }

        // Inserts a customization-override row (customize_id != base) directly, so the reader can be
        // exercised without a customize-write API (writing tenant overrides is out of this phase).
        private void SeedCustomizeRow(DatabaseType databaseType, string defineType, string customizeId, string defineKey, string contentXml)
        {
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(databaseType));
            string now = DbDialectRegistry.Get(databaseType).GetDefaultValueExpression(FieldDbType.DateTime);
            string tbl = databaseType.QuoteIdentifier("st_define");
            string type = databaseType.QuoteIdentifier("define_type");
            string cust = databaseType.QuoteIdentifier("customize_id");
            string key = databaseType.QuoteIdentifier("define_key");
            string content = databaseType.QuoteIdentifier("content");
            string upd = databaseType.QuoteIdentifier("sys_update_time");
            dbAccess.ExecuteNonQuery(
                $"INSERT INTO {tbl} ({type}, {cust}, {key}, {content}, {upd}) VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {now})",
                defineType, customizeId, defineKey, contentXml);
        }

        // The customize reader returns the per-customizeId override row; a different customizeId or the
        // base layer are isolated (distinct PK rows in the same table).
        private void RunCustomizeOverlay(DatabaseType databaseType)
        {
            var storage = NewStorage(databaseType);
            string customizeId = "cust_" + Guid.NewGuid().ToString("N");

            // FormLayout override: missing → null, then resolves after seeding.
            string layoutId = "RTL_" + Guid.NewGuid().ToString("N");
            Assert.Null(storage.GetCustomizeFormLayout(customizeId, layoutId));
            SeedCustomizeRow(databaseType, "FormLayout", customizeId, layoutId,
                XmlCodec.Serialize(new FormLayout { LayoutId = layoutId }));
            Assert.Equal(layoutId, storage.GetCustomizeFormLayout(customizeId, layoutId)!.LayoutId);
            // A different tenant has no override.
            Assert.Null(storage.GetCustomizeFormLayout("other_" + Guid.NewGuid().ToString("N"), layoutId));

            // Base layer is isolated from the customize layer (same define_key, different customize_id).
            storage.SaveFormLayout(new FormLayout { LayoutId = layoutId });
            Assert.NotNull(storage.GetFormLayout(layoutId));
            Assert.NotNull(storage.GetCustomizeFormLayout(customizeId, layoutId));

            // Language override (composite "lang.ns" key).
            string lang = string.Concat("rt-", Guid.NewGuid().ToString("N").AsSpan(0, 8));
            const string ns = "common";
            SeedCustomizeRow(databaseType, "LanguageResource", customizeId, $"{lang}.{ns}",
                XmlCodec.Serialize(new LanguageResource { Lang = lang, Namespace = ns }));
            Assert.Equal(lang, storage.GetCustomizeLanguage(customizeId, lang, ns)!.Lang);

            // ProgramSettings override (singleton key "*").
            Assert.Null(storage.GetCustomizeProgramSettings(customizeId));
            SeedCustomizeRow(databaseType, "ProgramSettings", customizeId, "*",
                XmlCodec.Serialize(new ProgramSettings()));
            Assert.NotNull(storage.GetCustomizeProgramSettings(customizeId));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：客製化 overlay 讀取 + base/租戶隔離")]
        public void CustomizeOverlay_SqlServer() => RunCustomizeOverlay(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：客製化 overlay 讀取 + base/租戶隔離")]
        public void CustomizeOverlay_PostgreSQL() => RunCustomizeOverlay(DatabaseType.PostgreSQL);
    }
}
