using System.ComponentModel;
using System.Data.Common;
using Bee.Db.Providers.Sqlite;

namespace Bee.Db.UnitTests.Manager
{
    /// <summary>
    /// 刻畫各 provider 的 <see cref="DbDataAdapter.UpdateBatchSize"/> 支援度。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>DbAccess.ApplySpec</c> 會嘗試開啟批次寫入，並在 provider 不支援時退回逐列。
    /// 它用的是<b>能力偵測</b>而不是一份寫死的 provider 清單 —— 清單會漂，也管不到宿主
    /// 自己替某個 <c>DatabaseType</c> 註冊了什麼 factory。
    /// </para>
    /// <para>
    /// 但實作的註解裡寫了「今天量到的是：SQL Server / MySQL / Oracle 接受，Npgsql 與框架自己的
    /// SQLite adapter 擲例外」。那句話沒有東西守著就會變成下一個過期的宣稱，所以這裡把它釘成測試。
    /// <b>某個 provider 哪天改了行為，這裡會紅</b> —— 那時要做的是重新量一次、更新註解，
    /// 而不是把測試改掉。
    /// </para>
    /// <para>
    /// 刻意直接建 factory 而不經 <c>DbProviderRegistry</c>：這裡驗的是套件本身的能力，
    /// 與部署註冊了誰無關，也就不需要容器。
    /// </para>
    /// </remarks>
    public class ProviderBatchingSupportTests
    {
        public static TheoryData<string, bool> Providers() => new()
        {
            { "SQLServer", true },
            { "MySQL", true },
            { "Oracle", true },
            { "PostgreSQL", false },
            { "SQLite", false },
        };

        private static DbProviderFactory CreateFactory(string name) => name switch
        {
            "SQLServer" => Microsoft.Data.SqlClient.SqlClientFactory.Instance,
            "MySQL" => MySqlConnector.MySqlConnectorFactory.Instance,
            "Oracle" => Oracle.ManagedDataAccess.Client.OracleClientFactory.Instance,
            "PostgreSQL" => Npgsql.NpgsqlFactory.Instance,
            "SQLite" => new SqliteProviderFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "未知的 provider。"),
        };

        [Theory]
        [MemberData(nameof(Providers))]
        [DisplayName("各 provider 的 adapter 對 UpdateBatchSize 的支援度應與實測基準相符")]
        public void Adapter_UpdateBatchSizeSupport_MatchesMeasuredBaseline(string provider, bool expectedSupport)
        {
            using var adapter = CreateFactory(provider).CreateDataAdapter();
            Assert.NotNull(adapter);

            var exception = Record.Exception(() => adapter!.UpdateBatchSize = 100);

            if (expectedSupport)
            {
                Assert.Null(exception);
                Assert.Equal(100, adapter!.UpdateBatchSize);
            }
            else
            {
                // 基底的 setter 擲 NotSupportedException —— 這正是 ApplySpec 用來偵測的訊號。
                Assert.IsType<NotSupportedException>(exception);
                Assert.Equal(1, adapter!.UpdateBatchSize);
            }
        }

        [Fact]
        [DisplayName("provider 清單不得為空（防空轉）")]
        public void Providers_AreNotEmpty()
        {
            Assert.NotEmpty(Providers());
        }
    }
}
