using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Database;
using Bee.Db.Manager;

namespace Bee.Db.UnitTests.Manager
{
    /// <summary>
    /// DbConnectionManager 的快取與連線資訊組裝測試。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這個類別自帶一份隔離的 <see cref="DatabaseSettings"/>（見
    /// <see cref="IsolatedDatabaseSettingsProvider"/>），<b>不碰 process-wide 的定義快取</b>。
    /// 先前它與 <c>DbAccessFactoryTests</c> 都對快取實例做 <c>Items.Add/Remove</c>，
    /// 平行執行下實測會擲 <c>ArgumentOutOfRangeException</c>。
    /// </para>
    /// <para>
    /// 這裡測的是連線字串組裝，本來就不需要資料庫，所以連 <c>SharedDbFixture</c> 也一併去掉。
    /// </para>
    /// </remarks>
    public sealed class DbConnectionManagerTests : IDisposable
    {
        private readonly IsolatedDatabaseSettingsProvider _provider = new();
        private readonly DbConnectionManagerService _manager;

        public DbConnectionManagerTests()
        {
            TestDbProviders.EnsureSqlServerRegistered();
            _manager = new DbConnectionManagerService(_provider);
        }

        /// <summary>
        /// 退訂 <c>GlobalEvents.DatabaseSettingsChanged</c>：static event 會抓著訂閱者不放，
        /// 每個測試類別留一個活的訂閱者，下一個測試的事件就會清到它。
        /// </summary>
        public void Dispose() => _manager.Dispose();

        private static string NewId(string label) => $"bee_dcm_{label}_{Guid.NewGuid():N}";

        private DatabaseItem AddItem(string id, Action<DatabaseItem> configure)
        {
            var item = new DatabaseItem { Id = id, DatabaseType = DatabaseType.SQLServer };
            configure(item);
            _provider.Settings.Items!.Add(item);
            return item;
        }

        private void RemoveItem(string id)
        {
            if (_provider.Settings.Items!.Contains(id))
                _provider.Settings.Items!.Remove(_provider.Settings.Items[id]!);
            _manager.Remove(id);
        }

        private DatabaseServer AddServer(string id, Action<DatabaseServer> configure)
        {
            var server = new DatabaseServer { Id = id, DatabaseType = DatabaseType.SQLServer };
            configure(server);
            _provider.Settings.Servers!.Add(server);
            return server;
        }

        private void RemoveServer(string id)
        {
            if (_provider.Settings.Servers!.Contains(id))
                _provider.Settings.Servers!.Remove(_provider.Settings.Servers[id]!);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("GetConnectionInfo 空白 databaseId 應拋 ArgumentNullException")]
        public void GetConnectionInfo_EmptyId_ThrowsArgumentNullException(string? id)
        {
            Assert.Throws<ArgumentNullException>(() => _manager.GetConnectionInfo(id!));
        }

        [Fact]
        [DisplayName("GetConnectionInfo 未定義的 databaseId 應拋 KeyNotFoundException")]
        public void GetConnectionInfo_UnknownId_ThrowsKeyNotFoundException()
        {
            // KeyedCollection 的 indexer 在找不到時直接拋出 KeyNotFoundException；
            // 原始碼的 null 檢查實際上不會被命中。
            var id = NewId("unknown");
            Assert.Throws<KeyNotFoundException>(() => _manager.GetConnectionInfo(id));
        }

        [Fact]
        [DisplayName("GetConnectionInfo 連線字串為空時應拋 InvalidOperationException")]
        public void GetConnectionInfo_EmptyConnectionString_ThrowsInvalidOperationException()
        {
            var id = NewId("emptyconn");
            AddItem(id, i => i.ConnectionString = string.Empty);
            try
            {
                Assert.Throws<InvalidOperationException>(() => _manager.GetConnectionInfo(id));
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
                var info = _manager.GetConnectionInfo(id);
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
                Assert.Throws<KeyNotFoundException>(() => _manager.GetConnectionInfo(id));
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
                var info = _manager.GetConnectionInfo(itemId);
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
                var info = _manager.GetConnectionInfo(itemId);
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
                var first = _manager.GetConnectionInfo(id);
                var second = _manager.GetConnectionInfo(id);
                Assert.Same(first, second);
                Assert.True(_manager.Contains(id));
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
                _manager.GetConnectionInfo(id);
                Assert.True(_manager.Contains(id));

                var removed = _manager.Remove(id);

                Assert.True(removed);
                Assert.False(_manager.Contains(id));
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
            Assert.False(_manager.Remove(id));
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
                _manager.GetConnectionInfo(id1);
                _manager.GetConnectionInfo(id2);

                _manager.Clear();

                Assert.False(_manager.Contains(id1));
                Assert.False(_manager.Contains(id2));
                Assert.Equal(0, _manager.Count);
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
                _manager.GetConnectionInfo(id);
                Assert.True(_manager.Contains(id));

                GlobalEvents.RaiseDatabaseSettingsChanged();

                Assert.False(_manager.Contains(id));
            }
            finally
            {
                RemoveItem(id);
            }
        }
    }
}
