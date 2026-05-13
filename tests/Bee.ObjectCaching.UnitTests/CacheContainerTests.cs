using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="ICacheContainer"/> 行為測試 —— 透過 fixture 的 DI 容器解析 cache instance，
    /// 不依賴 process-wide 靜態 facade，可與其他 test class 平行執行。
    /// </summary>
    public class CacheContainerTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public CacheContainerTests(SharedDbFixture fx) { _fx = fx; }

        private ICacheContainer Cache => _fx.GetRequiredService<ICacheContainer>();

        [Fact]
        [DisplayName("TableSchema.Get(categoryId, tableName) 應回傳對應 schema")]
        public void TableSchema_GetWithCategoryId_ReturnsSchema()
        {
            var schema = Cache.TableSchema.Get("common", "st_user");

            Assert.NotNull(schema);
            Assert.Equal("st_user", schema!.TableName, ignoreCase: true);
        }

        [Fact]
        [DisplayName("TableSchema.Get 以 DbCategoryIds.Common 作為系統資料庫應回傳 schema")]
        public void TableSchema_GetWithCommonDatabase_ReturnsSchema()
        {
            // framework 慣例：CategoryId="common" 的 DatabaseItem 其 Id 也為 "common"
            var schema = Cache.TableSchema.Get(DbCategoryIds.Common, "st_user");

            Assert.NotNull(schema);
            Assert.Equal("st_user", schema!.TableName, ignoreCase: true);
        }

        [Fact]
        [DisplayName("FormSchema.Get 應回傳對應 progId 的 schema")]
        public void FormSchema_ExistingProgId_ReturnsSchema()
        {
            var schema = Cache.FormSchema.Get("Department");

            Assert.NotNull(schema);
            Assert.Equal("Department", schema!.ProgId);
        }

        [Fact]
        [DisplayName("DbCategorySettings.Get 應回傳定義過的 category 設定")]
        public void DbCategorySettings_Get_ReturnsSettings()
        {
            var settings = Cache.DbCategorySettings.Get();
            Assert.NotNull(settings);
        }

        [Fact]
        [DisplayName("SessionInfo Set 後 Get 應回傳同一物件,Remove 後應為 null")]
        public void SessionInfo_SetGetRemove_BehavesCorrectly()
        {
            var token = Guid.NewGuid();
            var info = new SessionInfo
            {
                AccessToken = token,
                UserId = "u1",
                UserName = "User One"
            };

            Cache.SessionInfo.Set(info);
            var fromCache = Cache.SessionInfo.Get(token);
            Assert.NotNull(fromCache);
            Assert.Equal(token, fromCache!.AccessToken);

            Cache.SessionInfo.Remove(token);
            Assert.Null(Cache.SessionInfo.Get(token));
        }

        [Fact]
        [DisplayName("ProgramSettings.Get 於 tests/Define 無 ProgramSettings.xml 應拋 FileNotFoundException")]
        public void ProgramSettings_NoSettingsFile_ThrowsFileNotFound()
        {
            // tests/Define 下未放 ProgramSettings.xml,ProgramSettingsCache.CreateInstance 會拋
            // FileNotFoundException; 目的是讓 ProgramSettingsCache.Get 的 file-load 路徑被覆蓋。
            Assert.Throws<FileNotFoundException>(() => Cache.ProgramSettings.Get());
        }

        [Fact]
        [DisplayName("FormLayout.Get 於未定義 layoutId 應拋 FileNotFoundException")]
        public void FormLayout_UnknownLayoutId_ThrowsFileNotFound()
        {
            // 指定不存在的 layoutId,FileDefineStorage.GetFormLayout 會對檔案路徑驗證失敗
            // 並拋 FileNotFoundException;目的是覆蓋 FormLayoutCache.Get 的 file-load 路徑。
            Assert.Throws<FileNotFoundException>(() =>
                Cache.FormLayout.Get("__non_existent_layout_" + Guid.NewGuid().ToString("N")));
        }
    }
}
