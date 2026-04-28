using System.ComponentModel;
using System.Data.Common;
using Bee.Db.Manager;
using Microsoft.Data.SqlClient;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    public class DbProviderRegistryTests
    {
        [Fact]
        [DisplayName("Register factory 為 null 應擲 ArgumentNullException")]
        public void Register_NullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DbProviderRegistry.Register(DatabaseType.SQLServer, null!));
        }

        [Fact]
        [DisplayName("Get 未註冊型別應擲 KeyNotFoundException")]
        public void Get_UnregisteredType_Throws()
        {
            // GlobalFixture 註冊全部既定 DatabaseType；改用 enum 範圍外的整數
            // 作為「永遠不會被註冊」的 placeholder。
            Assert.Throws<KeyNotFoundException>(() =>
                DbProviderRegistry.Get((DatabaseType)9999));
        }

        [Collection("Initialize")]
        public class WithInitializedFixture
        {
            [Fact]
            [DisplayName("Get 已註冊型別應回傳對應 factory（透過 fixture 註冊）")]
            public void Get_RegisteredType_ReturnsFactory()
            {
                var factory = DbProviderRegistry.Get(DatabaseType.SQLServer);

                Assert.NotNull(factory);
                Assert.IsType<DbProviderFactory>(factory, exactMatch: false);
            }

            [Fact]
            [DisplayName("Register 重複呼叫應以新值取代舊值")]
            public void Register_ReplacesExistingFactory()
            {
                // 紀錄目前 factory，測試結束後還原
                var original = DbProviderRegistry.Get(DatabaseType.SQLServer);
                try
                {
                    DbProviderRegistry.Register(DatabaseType.SQLServer, SqlClientFactory.Instance);
                    var fetched = DbProviderRegistry.Get(DatabaseType.SQLServer);

                    Assert.Same(SqlClientFactory.Instance, fetched);
                }
                finally
                {
                    DbProviderRegistry.Register(DatabaseType.SQLServer, original);
                }
            }
        }
    }
}
