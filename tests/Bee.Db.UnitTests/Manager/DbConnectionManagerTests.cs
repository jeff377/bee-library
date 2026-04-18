using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Db.Manager;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests.Manager
{
    /// <summary>
    /// DbConnectionManager 的快取與連線資訊組裝測試。
    /// 使用唯一 databaseId 以避免與其他測試共用的全域快取互相干擾。
    /// </summary>
    [Collection("Initialize")]
    public class DbConnectionManagerTests
    {
        private static string NewId(string label) => $"bee_dcm_{label}_{Guid.NewGuid():N}";

        private static DatabaseItem AddItem(string id, Action<DatabaseItem> configure)
        {
            var settings = BackendInfo.DefineAccess.GetDatabaseSettings();
            var item = new DatabaseItem { Id = id, DatabaseType = DatabaseType.SQLServer };
            configure(item);
            settings.Items!.Add(item);
            return item;
        }

        private static void RemoveItem(string id)
        {
            var settings = BackendInfo.DefineAccess.GetDatabaseSettings();
            if (settings.Items!.Contains(id))
                settings.Items!.Remove(settings.Items[id]!);
            DbConnectionManager.Remove(id);
        }

        private static DatabaseServer AddServer(string id, Action<DatabaseServer> configure)
        {
            var settings = BackendInfo.DefineAccess.GetDatabaseSettings();
            var server = new DatabaseServer { Id = id, DatabaseType = DatabaseType.SQLServer };
            configure(server);
            settings.Servers!.Add(server);
            return server;
        }

        private static void RemoveServer(string id)
        {
            var settings = BackendInfo.DefineAccess.GetDatabaseSettings();
            if (settings.Servers!.Contains(id))
                settings.Servers!.Remove(settings.Servers[id]!);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("GetConnectionInfo 空白 databaseId 應拋 ArgumentNullException")]
        public void GetConnectionInfo_EmptyId_ThrowsArgumentNullException(string? id)
        {
            Assert.Throws<ArgumentNullException>(() => DbConnectionManager.GetConnectionInfo(id!));
        }

        [Fact]
        [DisplayName("GetConnectionInfo 未定義的 databaseId 應拋 KeyNotFoundException")]
        public void GetConnectionInfo_UnknownId_ThrowsKeyNotFoundException()
        {
            // KeyedCollection 的 indexer 在找不到時直接拋出 KeyNotFoundException；
            // 原始碼的 null 檢查實際上不會被命中。
            var id = NewId("unknown");
            Assert.Throws<KeyNotFoundException>(() => DbConnectionManager.GetConnectionInfo(id));
        }

        [Fact]
        [DisplayName("GetConnectionInfo 連線字串為空時應拋 InvalidOperationException")]
        public void GetConnectionInfo_EmptyConnectionString_ThrowsInvalidOperationException()
        {
            var id = NewId("emptyconn");
            AddItem(id, i => i.ConnectionString = string.Empty);
            try
            {
                Assert.Throws<InvalidOperationException>(() => DbConnectionManager.GetConnectionInfo(id));
            }
            finally
            {
                RemoveItem(id);
            }
        }

        [Fact]
        [DisplayName("GetConnectionInfo 替換 {@DbName}/{@UserId}/{@Password} 佔位符")]
        public void GetConnectionInfo_ReplacesAllPlaceholders()
        {
            var id = NewId("placeholder");
            AddItem(id, i =>
            {
                i.ConnectionString = "Server=x;Database={@DbName};User Id={@UserId};Password={@Password};";
                i.DbName = "db_v";
                i.UserId = "user_v";
                i.Password = "pwd_v";
            });
            try
            {
                var info = DbConnectionManager.GetConnectionInfo(id);
                Assert.Contains("db_v", info.ConnectionString);
                Assert.Contains("user_v", info.ConnectionString);
                Assert.Contains("pwd_v", info.ConnectionString);
                Assert.DoesNotContain("{@DbName}", info.ConnectionString);
                Assert.DoesNotContain("{@UserId}", info.ConnectionString);
                Assert.DoesNotContain("{@Password}", info.ConnectionString);
            }
            finally
            {
                RemoveItem(id);
            }
        }

        [Fact]
        [DisplayName("GetConnectionInfo 指定不存在的 ServerId 應拋 KeyNotFoundException")]
        public void GetConnectionInfo_ServerIdNotFound_ThrowsKeyNotFoundException()
        {
            // Servers 集合 indexer 也是 KeyedCollection 行為；若傳入未登記的 ServerId 會直接拋 KeyNotFoundException。
            var id = NewId("missingserver");
            AddItem(id, i =>
            {
                i.ServerId = "non_existent_server_" + Guid.NewGuid().ToString("N");
                i.ConnectionString = "Server=x;";
            });
            try
            {
                Assert.Throws<KeyNotFoundException>(() => DbConnectionManager.GetConnectionInfo(id));
            }
            finally
            {
                RemoveItem(id);
            }
        }

        [Fact]
        [DisplayName("GetConnectionInfo 透過 ServerId 應使用 Server 連線字串與 DatabaseType")]
        public void GetConnectionInfo_ServerId_UsesServerSettings()
        {
            var serverId = NewId("svr");
            var itemId = NewId("itemref");
            AddServer(serverId, s =>
            {
                s.ConnectionString = "Server=srv_host;UserId={@UserId};";
                s.DatabaseType = DatabaseType.SQLServer;
                s.UserId = "srv_user";
                s.Password = "srv_pwd";
            });
            AddItem(itemId, i =>
            {
                i.ServerId = serverId;
                // ConnectionString 無關緊要（會被 Server 覆蓋）
                i.ConnectionString = "ignored";
            });
            try
            {
                var info = DbConnectionManager.GetConnectionInfo(itemId);
                Assert.Contains("srv_host", info.ConnectionString);
                Assert.Contains("srv_user", info.ConnectionString);
                Assert.Equal(DatabaseType.SQLServer, info.DatabaseType);
            }
            finally
            {
                RemoveItem(itemId);
                RemoveServer(serverId);
            }
        }

        [Fact]
        [DisplayName("GetConnectionInfo ServerId 模式下 DatabaseItem 的 UserId/Password 應覆寫 Server 值")]
        public void GetConnectionInfo_ServerId_ItemOverridesServerUserPassword()
        {
            var serverId = NewId("svr2");
            var itemId = NewId("override");
            AddServer(serverId, s =>
            {
                s.ConnectionString = "Server=x;User Id={@UserId};Password={@Password};";
                s.UserId = "srv_user";
                s.Password = "srv_pwd";
            });
            AddItem(itemId, i =>
            {
                i.ServerId = serverId;
                i.UserId = "item_user";
                i.Password = "item_pwd";
            });
            try
            {
                var info = DbConnectionManager.GetConnectionInfo(itemId);
                Assert.Contains("item_user", info.ConnectionString);
                Assert.Contains("item_pwd", info.ConnectionString);
                Assert.DoesNotContain("srv_user", info.ConnectionString);
                Assert.DoesNotContain("srv_pwd", info.ConnectionString);
            }
            finally
            {
                RemoveItem(itemId);
                RemoveServer(serverId);
            }
        }

        [Fact]
        [DisplayName("GetConnectionInfo 同 databaseId 重複呼叫應回傳同一快取實例")]
        public void GetConnectionInfo_RepeatedCall_ReturnsCachedInstance()
        {
            var id = NewId("cache");
            AddItem(id, i => i.ConnectionString = "Server=abc;");
            try
            {
                var first = DbConnectionManager.GetConnectionInfo(id);
                var second = DbConnectionManager.GetConnectionInfo(id);
                Assert.Same(first, second);
                Assert.True(DbConnectionManager.Contains(id));
            }
            finally
            {
                RemoveItem(id);
            }
        }

        [Fact]
        [DisplayName("Remove 已快取者應回傳 true 且不再被 Contains")]
        public void Remove_CachedItem_RemovesFromCache()
        {
            var id = NewId("remove");
            AddItem(id, i => i.ConnectionString = "Server=abc;");
            try
            {
                DbConnectionManager.GetConnectionInfo(id);
                Assert.True(DbConnectionManager.Contains(id));

                var removed = DbConnectionManager.Remove(id);

                Assert.True(removed);
                Assert.False(DbConnectionManager.Contains(id));
            }
            finally
            {
                RemoveItem(id);
            }
        }

        [Fact]
        [DisplayName("Remove 未快取者應回傳 false")]
        public void Remove_NotCachedItem_ReturnsFalse()
        {
            var id = NewId("notcached");
            Assert.False(DbConnectionManager.Remove(id));
        }

        [Fact]
        [DisplayName("Clear 應清空所有快取條目")]
        public void Clear_EmptiesAllCachedEntries()
        {
            var id1 = NewId("clr1");
            var id2 = NewId("clr2");
            AddItem(id1, i => i.ConnectionString = "Server=a;");
            AddItem(id2, i => i.ConnectionString = "Server=b;");
            try
            {
                DbConnectionManager.GetConnectionInfo(id1);
                DbConnectionManager.GetConnectionInfo(id2);

                DbConnectionManager.Clear();

                Assert.False(DbConnectionManager.Contains(id1));
                Assert.False(DbConnectionManager.Contains(id2));
                Assert.Equal(0, DbConnectionManager.Count);
            }
            finally
            {
                RemoveItem(id1);
                RemoveItem(id2);
            }
        }

        [Fact]
        [DisplayName("DatabaseSettingsChanged 事件應清空快取")]
        public void RaiseDatabaseSettingsChanged_ClearsCache()
        {
            var id = NewId("event");
            AddItem(id, i => i.ConnectionString = "Server=a;");
            try
            {
                DbConnectionManager.GetConnectionInfo(id);
                Assert.True(DbConnectionManager.Contains(id));

                GlobalEvents.RaiseDatabaseSettingsChanged();

                Assert.False(DbConnectionManager.Contains(id));
            }
            finally
            {
                RemoveItem(id);
            }
        }
    }
}
