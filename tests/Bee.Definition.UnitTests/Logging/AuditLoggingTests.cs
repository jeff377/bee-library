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
        [DisplayName("GetColumns 應含共通欄位（含去正規化 user_name/company_name）並附加子類欄位")]
        public void GetColumns_IncludesCommonAndSubclassColumns()
        {
            var rowId = Guid.NewGuid();
            var entry = new TestAuditEntry
            {
                SysRowId = rowId,
                UserId = "demo",
                UserName = "Demo User",
                CompanyId = "c1",
                CompanyName = "Company One",
                Source = "Test.Action",
                Extra = "x",
            };

            var map = entry.GetColumns().ToDictionary(c => c.Name, c => c.Value);

            Assert.Equal("st_log_test", entry.TableName);
            Assert.Equal(rowId, map["sys_rowid"]);
            Assert.True(map.ContainsKey("log_time"));
            Assert.Equal("demo", map["user_id"]);
            Assert.Equal("Demo User", map["user_name"]);
            Assert.Equal("c1", map["company_id"]);
            Assert.Equal("Company One", map["company_name"]);
            Assert.Equal("Test.Action", map["source"]);
            Assert.Equal("x", map["extra"]);
            // Log rows are self-sufficient — no bare user_rowid that would need a join.
            Assert.False(map.ContainsKey("user_rowid"));
        }

        [Fact]
        [DisplayName("GetColumns 順序：sys_rowid 為第一欄、子類欄位在最後")]
        public void GetColumns_OrdersCommonFirstThenSubclass()
        {
            var columns = new TestAuditEntry { Extra = "x" }.GetColumns();

            Assert.Equal("sys_rowid", columns[0].Name);
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
        [DisplayName("LoginAuditEntry 目標表與 event/fail_reason 欄位正確")]
        public void LoginAuditEntry_ColumnsAndTable()
        {
            var entry = new LoginAuditEntry
            {
                UserId = "demo",
                UserName = "Demo User",
                Event = LoginEvent.LoginFailed,
                FailReason = "Invalid username or password.",
            };

            var map = entry.GetColumns().ToDictionary(c => c.Name, c => c.Value);

            Assert.Equal("st_log_login", entry.TableName);
            Assert.Equal((int)LoginEvent.LoginFailed, map["event"]);
            Assert.Equal("Invalid username or password.", map["fail_reason"]);
            Assert.Equal("demo", map["user_id"]);
            Assert.Equal("Demo User", map["user_name"]);
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
