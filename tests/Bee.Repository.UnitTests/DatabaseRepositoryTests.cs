using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.System;
using Bee.Repository.Provider;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="IDatabaseRepository"/> 預設實作的純邏輯測試。
    /// 透過 <see cref="SystemRepositoryProvider"/> 取得實例（避免直接依賴 internal 型別）。
    /// </summary>
    [Collection("Initialize")]
    public class DatabaseRepositoryTests
    {
        private const string ValidDatabaseId = "common";
        private const string ValidDbName = "DbName";
        private const string ValidTableName = "TableName";

        private static IDatabaseRepository CreateRepository()
            => new SystemRepositoryProvider().DatabaseRepository;

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("UpgradeTableSchema 空白 databaseId 應拋 ArgumentException")]
        public void UpgradeTableSchema_EmptyDatabaseId_ThrowsArgumentException(string? databaseId)
        {
            var repo = CreateRepository();
            Assert.Throws<ArgumentException>(() => repo.UpgradeTableSchema(databaseId!, ValidDbName, ValidTableName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("UpgradeTableSchema 空白 dbName 應拋 ArgumentException")]
        public void UpgradeTableSchema_EmptyDbName_ThrowsArgumentException(string? dbName)
        {
            var repo = CreateRepository();
            Assert.Throws<ArgumentException>(() => repo.UpgradeTableSchema(ValidDatabaseId, dbName!, ValidTableName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("UpgradeTableSchema 空白 tableName 應拋 ArgumentException")]
        public void UpgradeTableSchema_EmptyTableName_ThrowsArgumentException(string? tableName)
        {
            var repo = CreateRepository();
            Assert.Throws<ArgumentException>(() => repo.UpgradeTableSchema(ValidDatabaseId, ValidDbName, tableName!));
        }

        [Fact]
        [DisplayName("TestConnection 傳入未註冊的 DatabaseType 應拋 KeyNotFoundException")]
        public void TestConnection_UnregisteredDatabaseType_ThrowsKeyNotFoundException()
        {
            // GlobalFixture 僅註冊 SQLServer；MySQL/SQLite/Oracle 皆未註冊。
            var repo = CreateRepository();
            var item = new DatabaseItem
            {
                Id = "mysql_test",
                DatabaseType = DatabaseType.MySQL,
                ConnectionString = "Server=localhost;Database=foo;"
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
    }
}
