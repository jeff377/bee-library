using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Business.System;
using Bee.Definition;
using Bee.Definition.Settings;
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
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectDefineTests(SharedDbFixture fx) { _fx = fx; }
        [Fact]
        [DisplayName("GetCommonConfiguration 應回傳非空 XML")]
        public void GetCommonConfiguration_ReturnsNonEmptyXml()
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty, SysProgIds.System);

            var result = bo.GetCommonConfiguration(new GetCommonConfigurationArgs());

            Assert.False(string.IsNullOrWhiteSpace(result.CommonConfiguration));
        }

        [Fact]
        [DisplayName("GetDefine 本地呼叫 DatabaseSettings 應回傳 XML")]
        public void GetDefine_LocalCallDatabaseSettings_ReturnsXml()
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty, SysProgIds.System, isLocalCall: true);
            var args = new GetDefineArgs { DefineType = DefineType.DatabaseSettings };

            var result = bo.GetDefine(args);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.Xml));
        }

        [Fact]
        [DisplayName("GetDefine(DatabaseSettings) 應回傳原始檔，不得回傳快取的解密實例")]
        public void GetDefine_DatabaseSettings_ServesAsStoredNotTheDecryptedCache()
        {
            // 快取實例在 GetDatabaseSettings() 的 DecryptInPlace 之後持有明文密碼；
            // GetDefine 的契約是「定義如其所存」，故必須讀原始檔而非取快取，
            // 否則回應會夾帶明文憑證。
            var access = _fx.GetRequiredService<IDefineAccess>();
            var cached = access.GetDatabaseSettings();          // 觸發解密，快取轉為明文
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty, SysProgIds.System, isLocalCall: true);

            var xml = bo.GetDefine(new GetDefineArgs { DefineType = DefineType.DatabaseSettings }).Xml;
            var served = XmlCodec.Deserialize<DatabaseSettings>(xml!);

            Assert.NotNull(served);
            Assert.NotSame(cached, served);
            // 回傳的每個密碼要嘛為空、要嘛維持 enc: 密文，絕不可是解密後的明文。
            foreach (var password in (served.Servers ?? []).Select(s => s.Password)
                         .Concat((served.Items ?? []).Select(i => i.Password)))
            {
                Assert.True(string.IsNullOrEmpty(password) || password.StartsWith("enc:", StringComparison.Ordinal),
                    $"密碼未維持 enc: 形式：{password}");
            }
        }

        [Fact]
        [DisplayName("GetDefine 本地呼叫 SystemSettings 應回傳 XML")]
        public void GetDefine_LocalCallSystemSettings_ReturnsXml()
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty, SysProgIds.System, isLocalCall: true);
            var args = new GetDefineArgs { DefineType = DefineType.SystemSettings };

            var result = bo.GetDefine(args);

            Assert.False(string.IsNullOrWhiteSpace(result.Xml));
        }

        [Fact]
        [DisplayName("SaveDefine 本地呼叫 DbCategorySettings 應成功執行 SaveDefineCore 路徑")]
        public void SaveDefine_LocalCallDbCategorySettings_Succeeds()
        {
            // 先用共享 fixture 取得 XML（讀路徑）
            var getBo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty, SysProgIds.System, isLocalCall: true);
            var getResult = getBo.GetDefine(new GetDefineArgs { DefineType = DefineType.DbCategorySettings });
            Assert.False(string.IsNullOrWhiteSpace(getResult.Xml));

            // SaveDefine 會寫檔；改用獨立 IDefineAccess（指向暫存資料夾）避免污染 tests/Define/。
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-define-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var tempPaths = new PathOptions { DefinePath = tempDir };
                var tempAccess = new CacheDefineAccess(new FileDefineStorage(tempPaths), tempPaths);
                var saveBo = new SystemBusinessObject(
                    TestBeeContext.CreateWithDefineAccess(_fx, tempAccess), Guid.Empty, SysProgIds.System, isLocalCall: true);

                var saveResult = saveBo.SaveDefine(new SaveDefineArgs
                {
                    DefineType = DefineType.DbCategorySettings,
                    Xml = getResult.Xml
                });

                Assert.NotNull(saveResult);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }
    }
}
