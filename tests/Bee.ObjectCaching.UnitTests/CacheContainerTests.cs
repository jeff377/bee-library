using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Identity;

namespace Bee.ObjectCaching.UnitTests
{
    [Collection("Initialize")]
    public class CacheContainerTests
    {
        [Fact]
        [DisplayName("TableSchema.Get(dbName, tableName) 應回傳對應 schema")]
        public void TableSchema_GetWithDbName_ReturnsSchema()
        {
            var schema = CacheContainer.TableSchema.Get("common", "st_user");

            Assert.NotNull(schema);
            Assert.Equal("st_user", schema!.TableName, ignoreCase: true);
        }

        [Fact]
        [DisplayName("TableSchema.Get 以 BackendInfo.DatabaseId 作為預設資料庫應回傳 schema")]
        public void TableSchema_GetWithDefaultDatabase_ReturnsSchema()
        {
            // 由 SystemSettings.xml 內的 <DatabaseId>common</DatabaseId> 提供預設值
            var schema = CacheContainer.TableSchema.Get(BackendInfo.DatabaseId, "st_user");

            Assert.NotNull(schema);
            Assert.Equal("st_user", schema!.TableName, ignoreCase: true);
        }

        [Fact]
        [DisplayName("FormSchema.Get 應回傳對應 progId 的 schema")]
        public void FormSchema_ExistingProgId_ReturnsSchema()
        {
            var schema = CacheContainer.FormSchema.Get("Department");

            Assert.NotNull(schema);
            Assert.Equal("Department", schema!.ProgId);
        }

        [Fact]
        [DisplayName("DbSchemaSettings.Get 應回傳定義過的 schema 設定")]
        public void DbSchemaSettings_Get_ReturnsSettings()
        {
            var settings = CacheContainer.DbSchemaSettings.Get();
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

            CacheContainer.SessionInfo.Set(info);
            var fromCache = CacheContainer.SessionInfo.Get(token);
            Assert.NotNull(fromCache);
            Assert.Equal(token, fromCache!.AccessToken);

            CacheContainer.SessionInfo.Remove(token);
            Assert.Null(CacheContainer.SessionInfo.Get(token));
        }

        [Fact]
        [DisplayName("ProgramSettings.Get 於 tests/Define 無 ProgramSettings.xml 應拋 FileNotFoundException")]
        public void ProgramSettings_NoSettingsFile_ThrowsFileNotFound()
        {
            // tests/Define 下未放 ProgramSettings.xml,ProgramSettingsCache.CreateInstance 會拋 FileNotFoundException;
            // 目的是讓 ProgramSettingsCache.Get 的 file-load 路徑被執行並覆蓋。
            Assert.Throws<FileNotFoundException>(() => CacheContainer.ProgramSettings.Get());
        }

        [Fact]
        [DisplayName("FormLayout.Get 於未定義 layoutId 應拋 FileNotFoundException")]
        public void FormLayout_UnknownLayoutId_ThrowsFileNotFound()
        {
            // 指定不存在的 layoutId,FileDefineStorage.GetFormLayout 會對檔案路徑驗證失敗
            // 並拋 FileNotFoundException;目的是覆蓋 FormLayoutCache.Get 的 file-load 路徑。
            Assert.Throws<FileNotFoundException>(() =>
                CacheContainer.FormLayout.Get("__non_existent_layout_" + Guid.NewGuid().ToString("N")));
        }
    }
}
