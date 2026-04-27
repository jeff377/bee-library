using System.ComponentModel;
using System.Data.Common;
using Bee.Db.Manager;
using Microsoft.Data.SqlClient;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    public class DbProviderManagerTests
    {
        [Fact]
        [DisplayName("RegisterProvider factory 為 null 應擲 ArgumentNullException")]
        public void RegisterProvider_NullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                DbProviderManager.RegisterProvider(DatabaseType.SQLServer, null!));
        }

        [Fact]
        [DisplayName("GetFactory 未註冊型別應擲 KeyNotFoundException")]
        public void GetFactory_UnregisteredType_Throws()
        {
            // Oracle 在測試專案中未註冊（fixture 僅註冊 SQLServer）
            Assert.Throws<KeyNotFoundException>(() =>
                DbProviderManager.GetFactory(DatabaseType.Oracle));
        }

        [Collection("Initialize")]
        public class WithInitializedFixture
        {
            [Fact]
            [DisplayName("GetFactory 已註冊型別應回傳對應 factory（透過 fixture 註冊）")]
            public void GetFactory_RegisteredType_ReturnsFactory()
            {
                var factory = DbProviderManager.GetFactory(DatabaseType.SQLServer);

                Assert.NotNull(factory);
                Assert.IsType<DbProviderFactory>(factory, exactMatch: false);
            }

            [Fact]
            [DisplayName("RegisterProvider 重複呼叫應以新值取代舊值")]
            public void RegisterProvider_ReplacesExistingFactory()
            {
                // 紀錄目前 factory，測試結束後還原
                var original = DbProviderManager.GetFactory(DatabaseType.SQLServer);
                try
                {
                    DbProviderManager.RegisterProvider(DatabaseType.SQLServer, SqlClientFactory.Instance);
                    var fetched = DbProviderManager.GetFactory(DatabaseType.SQLServer);

                    Assert.Same(SqlClientFactory.Instance, fetched);
                }
                finally
                {
                    DbProviderManager.RegisterProvider(DatabaseType.SQLServer, original);
                }
            }
        }
    }
}
