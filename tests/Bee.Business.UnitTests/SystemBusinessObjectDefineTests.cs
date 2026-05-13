using System.ComponentModel;
using Bee.Business.System;
using Bee.Definition;
using Bee.Definition.Storage;
using Bee.ObjectCaching;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject"/> 與 <c>IDefineAccess</c>（透過 DI 解析）整合的純邏輯測試（記憶體存取，不走 DB）。
    /// </summary>
    public class SystemBusinessObjectDefineTests : IClassFixture<SharedDbFixture>
    {
        public SystemBusinessObjectDefineTests(SharedDbFixture _) { }

        [Fact]
        [DisplayName("GetCommonConfiguration 應回傳非空 XML")]
        public void GetCommonConfiguration_ReturnsNonEmptyXml()
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(), Guid.Empty);

            var result = bo.GetCommonConfiguration(new GetCommonConfigurationArgs());

            Assert.False(string.IsNullOrWhiteSpace(result.CommonConfiguration));
        }

        [Fact]
        [DisplayName("GetDefine 本地呼叫 DatabaseSettings 應回傳 XML")]
        public void GetDefine_LocalCallDatabaseSettings_ReturnsXml()
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(), Guid.Empty, isLocalCall: true);
            var args = new GetDefineArgs { DefineType = DefineType.DatabaseSettings };

            var result = bo.GetDefine(args);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.Xml));
        }

        [Fact]
        [DisplayName("GetDefine 本地呼叫 SystemSettings 應回傳 XML")]
        public void GetDefine_LocalCallSystemSettings_ReturnsXml()
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(), Guid.Empty, isLocalCall: true);
            var args = new GetDefineArgs { DefineType = DefineType.SystemSettings };

            var result = bo.GetDefine(args);

            Assert.False(string.IsNullOrWhiteSpace(result.Xml));
        }

        [Fact]
        [DisplayName("SaveDefine 本地呼叫 DbCategorySettings 應成功執行 SaveDefineCore 路徑")]
        public void SaveDefine_LocalCallDbCategorySettings_Succeeds()
        {
            // 先用共享 fixture 取得 XML（讀路徑）
            var getBo = new SystemBusinessObject(TestBeeContext.Create(), Guid.Empty, isLocalCall: true);
            var getResult = getBo.GetDefine(new GetDefineArgs { DefineType = DefineType.DbCategorySettings });
            Assert.False(string.IsNullOrWhiteSpace(getResult.Xml));

            // SaveDefine 會寫檔；改用獨立 IDefineAccess（指向暫存資料夾）避免污染 tests/Define/。
            using var temp = new TempDefinePath();
            var tempAccess = new LocalDefineAccess(new FileDefineStorage(temp.Options), temp.Options);
            var saveBo = new SystemBusinessObject(
                TestBeeContext.CreateWithDefineAccess(tempAccess), Guid.Empty, isLocalCall: true);

            var saveResult = saveBo.SaveDefine(new SaveDefineArgs
            {
                DefineType = DefineType.DbCategorySettings,
                Xml = getResult.Xml
            });

            Assert.NotNull(saveResult);
        }
    }
}
