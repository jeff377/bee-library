using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.LoadTests.Bootstrap;
using Bee.LoadTests.Configuration;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="DefineWorkspace"/>.
    /// </summary>
    public class DefineWorkspaceTests : IDisposable
    {
        private const string ConnectionString = "Server=localhost;Database={@DbName};";

        private readonly string _source = Path.Combine(
            Path.GetTempPath(), "bee-loadtest-src-" + Guid.NewGuid().ToString("N"));

        public DefineWorkspaceTests() => WriteSourceDefinitions();

        public void Dispose()
        {
            if (Directory.Exists(_source)) { Directory.Delete(_source, recursive: true); }
            GC.SuppressFinalize(this);
        }

        private void WriteSourceDefinitions()
        {
            Directory.CreateDirectory(_source);
            Directory.CreateDirectory(Path.Combine(_source, "FormSchema"));
            File.WriteAllText(Path.Combine(_source, "FormSchema", "marker.txt"), "nested");

            var databaseSettings = new DatabaseSettings();
            databaseSettings.Items!.Add(new DatabaseItem
            {
                Id = "common", CategoryId = "common",
                DatabaseType = DatabaseType.SQLite,
                ConnectionString = "Data Source=demo.db"
            });
            databaseSettings.Items!.Add(new DatabaseItem
            {
                Id = "company", CategoryId = "company",
                DatabaseType = DatabaseType.SQLite,
                ConnectionString = "Data Source=demo.db"
            });
            XmlCodec.SerializeToFile(databaseSettings,
                Path.Combine(_source, "DatabaseSettings.xml"));

            var systemSettings = new SystemSettings();
            systemSettings.CommonConfiguration.IsDebugMode = true;
            systemSettings.BackendConfiguration.AuditLogOptions.UseBackgroundWriter = false;
            XmlCodec.SerializeToFile(systemSettings,
                Path.Combine(_source, "SystemSettings.xml"));
        }

        private static LoadTestOptions CreateOptions(DatabaseType provider = DatabaseType.SQLServer)
            => new() { Database = new DatabaseOptions { Provider = provider } };

        private static DatabaseSettings ReadDatabaseSettings(DefineWorkspace workspace)
            => XmlCodec.DeserializeFromFile<DatabaseSettings>(
                Path.Combine(workspace.DefinePath, "DatabaseSettings.xml"))!;

        [Fact]
        [DisplayName("複製整棵定義樹，含子資料夾")]
        public void CreateFrom_CopiesNestedFiles()
        {
            using var workspace = DefineWorkspace.CreateFrom(_source, CreateOptions(), ConnectionString);

            Assert.True(File.Exists(Path.Combine(workspace.DefinePath, "FormSchema", "marker.txt")));
            Assert.NotEqual(_source, workspace.DefinePath);
        }

        [Fact]
        [DisplayName("每個 DatabaseItem 改寫為設定的 provider")]
        public void CreateFrom_RewritesProvider()
        {
            using var workspace = DefineWorkspace.CreateFrom(
                _source, CreateOptions(DatabaseType.PostgreSQL), ConnectionString);

            var settings = ReadDatabaseSettings(workspace);
            Assert.All(settings.Items!, item =>
                Assert.Equal(DatabaseType.PostgreSQL, item.DatabaseType));
        }

        [Fact]
        [DisplayName("連線字串的 {@DbName} 置換為帶前綴的資料庫名")]
        public void CreateFrom_SubstitutesDbNamePlaceholder()
        {
            using var workspace = DefineWorkspace.CreateFrom(_source, CreateOptions(), ConnectionString);

            var settings = ReadDatabaseSettings(workspace);
            Assert.Equal("Server=localhost;Database=loadtest_common;",
                settings.Items!["common"]!.ConnectionString);
            Assert.Equal("Server=localhost;Database=loadtest_company;",
                settings.Items!["company"]!.ConnectionString);
        }

        [Fact]
        [DisplayName("置換結果不得等於裸 CategoryId —— 那是單元測試自己的資料庫")]
        public void CreateFrom_NeverTargetsTheBareCategoryDatabase()
        {
            using var workspace = DefineWorkspace.CreateFrom(_source, CreateOptions(), ConnectionString);

            var settings = ReadDatabaseSettings(workspace);

            // The test harness creates catalogs named after the bare categories. A run that
            // resolved to one of those would seed rows into the databases the unit tests depend
            // on, and the damage would surface later, elsewhere, as unrelated test failures.
            Assert.All(settings.Items!, item =>
                Assert.DoesNotContain($"Database={item.CategoryId};", item.ConnectionString,
                    StringComparison.Ordinal));
        }

        [Fact]
        [DisplayName("自訂前綴會被沿用")]
        public void CreateFrom_HonoursCustomPrefix()
        {
            var options = CreateOptions();
            options.Database.DatabaseNamePrefix = "perf_";

            using var workspace = DefineWorkspace.CreateFrom(_source, options, ConnectionString);

            var settings = ReadDatabaseSettings(workspace);
            Assert.Equal("Server=localhost;Database=perf_common;",
                settings.Items!["common"]!.ConnectionString);
        }

        [Fact]
        [DisplayName("連線字串無 placeholder 時原樣沿用")]
        public void CreateFrom_WithoutPlaceholder_UsesStringAsIs()
        {
            const string plain = "Server=localhost;Database=bee;";

            using var workspace = DefineWorkspace.CreateFrom(_source, CreateOptions(), plain);

            var settings = ReadDatabaseSettings(workspace);
            Assert.All(settings.Items!, item => Assert.Equal(plain, item.ConnectionString));
        }

        [Fact]
        [DisplayName("關閉 debug 模式並開回 audit 背景寫入")]
        public void CreateFrom_OverridesDemoOnlySystemSettings()
        {
            using var workspace = DefineWorkspace.CreateFrom(_source, CreateOptions(), ConnectionString);

            var settings = XmlCodec.DeserializeFromFile<SystemSettings>(
                Path.Combine(workspace.DefinePath, "SystemSettings.xml"))!;

            Assert.False(settings.CommonConfiguration.IsDebugMode);
            Assert.True(settings.BackendConfiguration.AuditLogOptions.UseBackgroundWriter);
        }

        [Fact]
        [DisplayName("來源定義檔不被修改")]
        public void CreateFrom_LeavesSourceUntouched()
        {
            using (DefineWorkspace.CreateFrom(_source, CreateOptions(), ConnectionString))
            {
                // The workspace exists here; the assertions below run after it is disposed.
            }

            var settings = XmlCodec.DeserializeFromFile<DatabaseSettings>(
                Path.Combine(_source, "DatabaseSettings.xml"))!;
            Assert.All(settings.Items!, item =>
                Assert.Equal(DatabaseType.SQLite, item.DatabaseType));

            var system = XmlCodec.DeserializeFromFile<SystemSettings>(
                Path.Combine(_source, "SystemSettings.xml"))!;
            Assert.True(system.CommonConfiguration.IsDebugMode);
        }

        [Fact]
        [DisplayName("Dispose 後刪除暫存副本")]
        public void Dispose_RemovesTemporaryCopy()
        {
            string path;
            using (var workspace = DefineWorkspace.CreateFrom(_source, CreateOptions(), ConnectionString))
            {
                path = workspace.DefinePath;
                Assert.True(Directory.Exists(path));
            }

            Assert.False(Directory.Exists(path));
        }

        [Fact]
        [DisplayName("來源目錄不存在時擲出可辨識的例外")]
        public void CreateFrom_MissingSource_Throws()
        {
            var missing = Path.Combine(Path.GetTempPath(), "bee-loadtest-absent-" + Guid.NewGuid());

            Assert.Throws<DirectoryNotFoundException>(
                () => DefineWorkspace.CreateFrom(missing, CreateOptions(), ConnectionString));
        }

        [Fact]
        [DisplayName("實際的 Northwind 定義檔可被複製並改寫為非 SQLite")]
        public void CreateFrom_RealNorthwindDefinitions_RewritesEveryCategory()
        {
            var source = LocateNorthwindDefine();
            Assert.True(Directory.Exists(source), $"Northwind definitions not found at '{source}'.");

            using var workspace = DefineWorkspace.CreateFrom(
                source, CreateOptions(DatabaseType.SQLServer), ConnectionString);

            var settings = ReadDatabaseSettings(workspace);

            // The shipped set binds common / company / log, all to SQLite. Every one of them must
            // come back pointed at the configured provider: a category left on SQLite would send
            // part of the run to an engine the load test excludes.
            Assert.Equal(3, settings.Items!.Count);
            Assert.All(settings.Items!, item =>
                Assert.Equal(DatabaseType.SQLServer, item.DatabaseType));

            var system = XmlCodec.DeserializeFromFile<SystemSettings>(
                Path.Combine(workspace.DefinePath, "SystemSettings.xml"))!;
            Assert.False(system.CommonConfiguration.IsDebugMode);
            Assert.True(system.BackendConfiguration.AuditLogOptions.UseBackgroundWriter);

            // The definitions the scenarios need must survive the copy.
            Assert.True(File.Exists(Path.Combine(
                workspace.DefinePath, "FormSchema", "Order.FormSchema.xml")));
            Assert.True(File.Exists(Path.Combine(
                workspace.DefinePath, "TableSchema", "company", "ft_order_detail.TableSchema.xml")));
        }

        private static string LocateNorthwindDefine()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(
                    directory.FullName, "apps", "Bee.Northwind", "Define");
                if (Directory.Exists(candidate)) { return candidate; }
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException(
                "Could not locate 'apps/Bee.Northwind/Define' walking up from " +
                $"'{AppContext.BaseDirectory}'.");
        }

        [Theory]
        [InlineData(DatabaseType.SQLServer, "BEE_TEST_CONNSTR_SQLSERVER")]
        [InlineData(DatabaseType.PostgreSQL, "BEE_TEST_CONNSTR_POSTGRESQL")]
        [InlineData(DatabaseType.MySQL, "BEE_TEST_CONNSTR_MYSQL")]
        [InlineData(DatabaseType.Oracle, "BEE_TEST_CONNSTR_ORACLE")]
        [DisplayName("連線字串環境變數命名與 test.sh 慣例一致")]
        public void GetConnectionStringVariable_MatchesTestHarnessConvention(
            DatabaseType provider, string expected)
        {
            Assert.Equal(expected, DefineWorkspace.GetConnectionStringVariable(provider));
        }
    }
}
