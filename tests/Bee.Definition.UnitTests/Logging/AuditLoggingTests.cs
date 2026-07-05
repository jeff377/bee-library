using System.ComponentModel;
using Bee.Definition.Logging;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Logging
{
    /// <summary>
    /// <see cref="AuditEntry"/> / <see cref="NullAuditLogWriter"/> / <see cref="AuditLogOptions"/>
    /// 的單元測試：驗證共通欄位組裝、no-op writer 行為與預設選項。
    /// </summary>
    public class AuditLoggingTests
    {
        private sealed class TestAuditEntry : AuditEntry
        {
            public override string TableName => "st_log_test";

            public string? Extra { get; init; }

            protected override void AddColumns(IList<AuditColumn> columns)
                => columns.Add(new AuditColumn("extra", Extra));
        }

        [Fact]
        [DisplayName("GetColumns 應含共通欄位並附加子類欄位")]
        public void GetColumns_IncludesCommonAndSubclassColumns()
        {
            var userRowId = Guid.NewGuid();
            var entry = new TestAuditEntry
            {
                UserRowId = userRowId,
                UserId = "demo",
                CompanyId = "c1",
                Source = "Test.Action",
                Extra = "x",
            };

            var map = entry.GetColumns().ToDictionary(c => c.Name, c => c.Value);

            Assert.Equal("st_log_test", entry.TableName);
            Assert.True(map.ContainsKey("log_time"));
            Assert.Equal(userRowId, map["user_rowid"]);
            Assert.Equal("demo", map["user_id"]);
            Assert.Equal("c1", map["company_id"]);
            Assert.Equal("Test.Action", map["source"]);
            Assert.Equal("x", map["extra"]);
        }

        [Fact]
        [DisplayName("GetColumns 順序：log_time 為第一欄、子類欄位在最後")]
        public void GetColumns_OrdersCommonFirstThenSubclass()
        {
            var columns = new TestAuditEntry { Extra = "x" }.GetColumns();

            Assert.Equal("log_time", columns[0].Name);
            Assert.Equal("extra", columns[^1].Name);
        }

        [Fact]
        [DisplayName("LogTimeUtc 預設為 UTC 時間")]
        public void LogTimeUtc_DefaultsToUtc()
        {
            var entry = new TestAuditEntry();

            Assert.Equal(DateTimeKind.Utc, entry.LogTimeUtc.Kind);
        }

        [Fact]
        [DisplayName("NullAuditLogWriter.Write 不應拋例外")]
        public void NullAuditLogWriter_Write_DoesNotThrow()
        {
            var exception = Record.Exception(() => NullAuditLogWriter.Instance.Write(new TestAuditEntry()));

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("AuditLogOptions 預設：關閉、背景寫入、檢視記錄關閉")]
        public void AuditLogOptions_Defaults()
        {
            var options = new AuditLogOptions();

            Assert.False(options.Enabled);
            Assert.True(options.UseBackgroundWriter);
            Assert.False(options.AccessEnabled);
        }
    }
}
