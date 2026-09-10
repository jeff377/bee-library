using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Security;
using Bee.LoadTests.Configuration;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="LoadTestOptions"/>.
    /// </summary>
    public class LoadTestOptionsTests
    {
        private static LoadTestOptions CreateValid() => new()
        {
            Database = new DatabaseOptions { Provider = DatabaseType.SQLServer },
            Scenarios = [new ScenarioOptions { Name = "GetList", Enabled = true, Weight = 1 }]
        };

        [Fact]
        [DisplayName("解析 JSON：列舉以字串表示、屬性大小寫不敏感")]
        public void Parse_ReadsStringEnumsCaseInsensitively()
        {
            const string json = """
                {
                  "Target": { "mode": "Remote", "endpoint": "http://localhost:5000/api",
                              "protectionLevel": "Public" },
                  "load": { "virtualUsers": 25, "model": "Open" },
                  "auth": { "tokenStrategy": "Shared" },
                  "scenarios": [ { "name": "GetList", "enabled": true, "weight": 3 } ]
                }
                """;

            var options = LoadTestOptions.Parse(json);

            Assert.Equal(TargetMode.Remote, options.Target.Mode);
            Assert.Equal(ApiProtectionLevel.Public, options.Target.ProtectionLevel);
            Assert.Equal(LoadModel.Open, options.Load.Model);
            Assert.Equal(TokenStrategy.Shared, options.Auth.TokenStrategy);
            Assert.Equal(25, options.Load.VirtualUsers);
            Assert.Equal(3, options.Scenarios[0].Weight);
        }

        [Fact]
        [DisplayName("解析 JSON：允許註解與尾端逗號")]
        public void Parse_AllowsCommentsAndTrailingCommas()
        {
            const string json = """
                {
                  // the sample file ships with comments
                  "scenarios": [ { "name": "GetList" }, ],
                }
                """;

            var options = LoadTestOptions.Parse(json);

            Assert.Single(options.Scenarios);
        }

        [Fact]
        [DisplayName("未指定的區段採預設值")]
        public void Parse_MissingSections_UseDefaults()
        {
            var options = LoadTestOptions.Parse("{}");

            Assert.Equal(TargetMode.Local, options.Target.Mode);
            Assert.Equal(DatabaseType.SQLServer, options.Database.Provider);
            Assert.Equal(LoadModel.Closed, options.Load.Model);
            Assert.Equal([50d, 95d, 99d], options.Report.Percentiles);
        }

        [Fact]
        [DisplayName("驗證：SQLite 一律拒絕，且訊息說明原因")]
        public void Validate_SqliteProvider_Throws()
        {
            var options = CreateValid();
            options.Database.Provider = DatabaseType.SQLite;

            var ex = Assert.Throws<InvalidOperationException>(options.Validate);
            Assert.Contains("SQLite", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("驗證：Remote 模式未給 endpoint 時拒絕")]
        public void Validate_RemoteWithoutEndpoint_Throws()
        {
            var options = CreateValid();
            options.Target.Mode = TargetMode.Remote;

            Assert.Throws<InvalidOperationException>(options.Validate);
        }

        [Fact]
        [DisplayName("驗證：Local 模式不需要 endpoint")]
        public void Validate_LocalWithoutEndpoint_Passes()
        {
            var options = CreateValid();
            options.Target.Mode = TargetMode.Local;

            options.Validate();
        }

        [Fact]
        [DisplayName("驗證：沒有任何啟用的場景時拒絕")]
        public void Validate_NoEnabledScenario_Throws()
        {
            var options = CreateValid();
            options.Scenarios[0].Enabled = false;

            Assert.Throws<InvalidOperationException>(options.Validate);
        }

        [Fact]
        [DisplayName("驗證：啟用的場景權重必須為正")]
        public void Validate_EnabledScenarioWithZeroWeight_Throws()
        {
            var options = CreateValid();
            options.Scenarios[0].Weight = 0;

            var ex = Assert.Throws<InvalidOperationException>(options.Validate);
            Assert.Contains("GetList", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("驗證：停用的場景不檢查權重")]
        public void Validate_DisabledScenarioWithZeroWeight_Ignored()
        {
            var options = CreateValid();
            options.Scenarios.Add(new ScenarioOptions { Name = "Save", Enabled = false, Weight = 0 });

            options.Validate();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [DisplayName("驗證：VU 數必須為正")]
        public void Validate_NonPositiveVirtualUsers_Throws(int virtualUsers)
        {
            var options = CreateValid();
            options.Load.VirtualUsers = virtualUsers;

            Assert.Throws<InvalidOperationException>(options.Validate);
        }

        [Fact]
        [DisplayName("驗證：warm-up 可為 0 但不可為負")]
        public void Validate_WarmupSeconds_ZeroAllowedNegativeRejected()
        {
            var options = CreateValid();

            options.Load.WarmupSeconds = 0;
            options.Validate();

            options.Load.WarmupSeconds = -1;
            Assert.Throws<InvalidOperationException>(options.Validate);
        }

        [Fact]
        [DisplayName("驗證：資料庫名稱前綴不可為空")]
        public void Validate_EmptyDatabaseNamePrefix_Throws()
        {
            var options = CreateValid();
            options.Database.DatabaseNamePrefix = "";

            var ex = Assert.Throws<InvalidOperationException>(options.Validate);
            Assert.Contains("unit tests", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("load test_")]
        [InlineData("load-test_")]
        [InlineData("load];DROP DATABASE x--")]
        [DisplayName("驗證：前綴只接受字母數字底線（名稱會進入 DDL）")]
        public void Validate_UnsafeDatabaseNamePrefix_Throws(string prefix)
        {
            var options = CreateValid();
            options.Database.DatabaseNamePrefix = prefix;

            Assert.Throws<InvalidOperationException>(options.Validate);
        }

        [Fact]
        [DisplayName("資料庫名稱由前綴加上 CategoryId 組成")]
        public void ResolveDatabaseName_PrependsPrefix()
        {
            var database = new DatabaseOptions { DatabaseNamePrefix = "loadtest_" };

            Assert.Equal("loadtest_company", database.ResolveDatabaseName("company"));
            Assert.Equal("loadtest_common", database.ResolveDatabaseName("common"));
        }

        [Fact]
        [DisplayName("serve 監聽位址未設定時採用 loopback 預設埠")]
        public void ResolveServeUrl_NotConfigured_UsesLoopbackDefaultPort()
        {
            var target = new TargetOptions();

            Assert.Equal(
                $"http://localhost:{TargetOptions.DefaultServePort}", target.ResolveServeUrl());
        }

        [Fact]
        [DisplayName("serve 監聽位址已設定時原樣採用")]
        public void ResolveServeUrl_Configured_UsesConfiguredValue()
        {
            var target = new TargetOptions { ServeUrl = "http://0.0.0.0:8080" };

            Assert.Equal("http://0.0.0.0:8080", target.ResolveServeUrl());
        }

        [Fact]
        [DisplayName("範例設定檔可被解析且通過驗證")]
        public void SampleConfiguration_ParsesAndValidates()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "loadtest.sample.json");
            Assert.True(File.Exists(path), $"Sample configuration not found at '{path}'.");

            var options = LoadTestOptions.FromFile(path);

            options.Validate();
            Assert.NotEmpty(options.Scenarios);
        }
    }
}
