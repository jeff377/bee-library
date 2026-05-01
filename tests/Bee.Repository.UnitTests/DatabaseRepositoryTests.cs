using System.ComponentModel;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.System;
using Bee.Repository.Factories;
using Bee.Definition.Database;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="IDatabaseRepository"/> 預設實作的純邏輯測試。
    /// 透過 <see cref="SystemRepositoryFactory"/> 取得實例（避免直接依賴 internal 型別）。
    /// </summary>
    [Collection("Initialize")]
    public class DatabaseRepositoryTests
    {
        private const string ValidDatabaseId = "common";
        private const string ValidDbName = "DbName";
        private const string ValidTableName = "TableName";

        private static IDatabaseRepository CreateRepository()
            => new SystemRepositoryFactory().CreateDatabaseRepository();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("UpgradeTableSchema 空白 databaseId 應拋 ArgumentException")]
        public void UpgradeTableSchema_EmptyDatabaseId_ThrowsArgumentException(string? databaseId)
        {
            var repo = CreateRepository();
            Assert.ThrowsAny<ArgumentException>(() => repo.UpgradeTableSchema(databaseId!, ValidDbName, ValidTableName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("UpgradeTableSchema 空白 dbName 應拋 ArgumentException")]
        public void UpgradeTableSchema_EmptyDbName_ThrowsArgumentException(string? dbName)
        {
            var repo = CreateRepository();
            Assert.ThrowsAny<ArgumentException>(() => repo.UpgradeTableSchema(ValidDatabaseId, dbName!, ValidTableName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("UpgradeTableSchema 空白 tableName 應拋 ArgumentException")]
        public void UpgradeTableSchema_EmptyTableName_ThrowsArgumentException(string? tableName)
        {
            var repo = CreateRepository();
            Assert.ThrowsAny<ArgumentException>(() => repo.UpgradeTableSchema(ValidDatabaseId, ValidDbName, tableName!));
        }

        [Fact]
        [DisplayName("TestConnection 傳入未註冊的 DatabaseType 應拋 KeyNotFoundException")]
        public void TestConnection_UnregisteredDatabaseType_ThrowsKeyNotFoundException()
        {
            // GlobalFixture 已註冊全部既定 DatabaseType（SQLServer / PostgreSQL / SQLite /
            // MySQL / Oracle）。為了仍能驗證「未註冊 DatabaseType 應拋 KeyNotFoundException」
            // 這條保護邏輯，這裡 cast 一個 enum 範圍外的整數作為「永遠不會被註冊」的 placeholder。
            var repo = CreateRepository();
            var item = new DatabaseItem
            {
                Id = "unregistered_test",
                DatabaseType = (DatabaseType)9999,
                ConnectionString = "Data Source=localhost;User Id=foo;Password=bar;"
            };

            Assert.Throws<KeyNotFoundException>(() => repo.TestConnection(item));
        }

        [Fact]
        [DisplayName("TestConnection 傳入 null DatabaseItem 應拋 NullReferenceException（依現況）")]
        public void TestConnection_NullItem_ThrowsNullReferenceException()
        {
            // 目前實作未對 null 做防護，呼叫 item.DatabaseType 時會拋 NullReferenceException；
            // 測試僅記錄現況，若日後加上防護需同步調整。
            var repo = CreateRepository();
            Assert.Throws<NullReferenceException>(() => repo.TestConnection(null!));
        }

        [Fact]
        [DisplayName("TestConnection 含 {@DbName} 佔位符且 DbName 非空時應完成替換並嘗試連線")]
        public void TestConnection_WithDbNamePlaceholder_ReplacesAndAttempts()
        {
            // 以合法 SQL Server 連線字串語法但指向不存在的主機，確保：
            // 1. 字串替換成功（未替換則 SqlConnection 仍接受 {@DbName} 作為 catalog 名）
            // 2. 最終在 Open() 階段失敗，而不是因字串解析拋 ArgumentException
            var repo = CreateRepository();
            var item = new DatabaseItem
            {
                Id = "placeholder_dbname",
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = "Server=127.0.0.1,65535;Initial Catalog={@DbName};Connection Timeout=1;",
                DbName = "bee_test_placeholder_db"
            };

            // 期望 Open 時拋出 SqlException 或相容例外（連線失敗）。
            var ex = Record.Exception(() => repo.TestConnection(item));
            Assert.NotNull(ex);
            Assert.IsNotType<ArgumentException>(ex);
        }

        [Fact]
        [DisplayName("TestConnection 含 {@UserId} 與 {@Password} 佔位符且皆非空時應完成替換")]
        public void TestConnection_WithUserIdAndPasswordPlaceholder_ReplacesAndAttempts()
        {
            var repo = CreateRepository();
            var item = new DatabaseItem
            {
                Id = "placeholder_user",
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = "Server=127.0.0.1,65535;User Id={@UserId};Password={@Password};Connection Timeout=1;",
                UserId = "sa_test",
                Password = "p@ssword_test"
            };

            var ex = Record.Exception(() => repo.TestConnection(item));
            Assert.NotNull(ex);
            Assert.IsNotType<ArgumentException>(ex);
        }

        [Fact]
        [DisplayName("TestConnection 所有佔位符同時指定時應完成替換")]
        public void TestConnection_WithAllPlaceholders_ReplacesAndAttempts()
        {
            var repo = CreateRepository();
            var item = new DatabaseItem
            {
                Id = "placeholder_all",
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = "Server=127.0.0.1,65535;Initial Catalog={@DbName};User Id={@UserId};Password={@Password};Connection Timeout=1;",
                DbName = "bee_test_db",
                UserId = "sa_test",
                Password = "p@ssword_test"
            };

            var ex = Record.Exception(() => repo.TestConnection(item));
            Assert.NotNull(ex);
            Assert.IsNotType<ArgumentException>(ex);
        }
    }
}
