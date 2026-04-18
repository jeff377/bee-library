using System.ComponentModel;
using Bee.Definition;

namespace Bee.ObjectCaching.UnitTests
{
    [Collection("Initialize")]
    public class CacheFuncTests
    {
        [Fact]
        [DisplayName("GetTableSchema(dbName, tableName) 應回傳對應 schema")]
        public void GetTableSchema_WithDbName_ReturnsSchema()
        {
            var schema = CacheFunc.GetTableSchema("common", "st_user");

            Assert.NotNull(schema);
            Assert.Equal("st_user", schema!.TableName, ignoreCase: true);
        }

        [Fact]
        [DisplayName("GetTableSchema(tableName) 應使用預設資料庫並回傳 schema")]
        public void GetTableSchema_DefaultDatabase_ReturnsSchema()
        {
            // 由 SystemSettings.xml 內的 <DatabaseId>common</DatabaseId> 提供預設值
            var schema = CacheFunc.GetTableSchema("st_user");

            Assert.NotNull(schema);
            Assert.Equal("st_user", schema!.TableName, ignoreCase: true);
        }

        [Fact]
        [DisplayName("GetFormSchema 應回傳對應 progId 的 schema")]
        public void GetFormSchema_ExistingProgId_ReturnsSchema()
        {
            var schema = CacheFunc.GetFormSchema("Department");

            Assert.NotNull(schema);
            Assert.Equal("Department", schema!.ProgId);
        }

        [Fact]
        [DisplayName("GetDbSchemaSettings 應回傳定義過的 schema 設定")]
        public void GetDbSchemaSettings_ReturnsSettings()
        {
            var settings = CacheFunc.GetDbSchemaSettings();
            Assert.NotNull(settings);
        }

        [Fact]
        [DisplayName("ViewState 寫入後可正確取回")]
        public void SaveViewState_ThenLoad_ReturnsSameValue()
        {
            var key = Guid.NewGuid();
            var payload = new { Page = 5, Filter = "abc" };

            CacheFunc.SaveViewState(key, payload);
            var loaded = CacheFunc.LoadViewState(key);

            Assert.Same(payload, loaded);
        }

        [Fact]
        [DisplayName("LoadViewState 讀取不存在的 key 應回傳 null")]
        public void LoadViewState_MissingKey_ReturnsNull()
        {
            var key = Guid.NewGuid();
            Assert.Null(CacheFunc.LoadViewState(key));
        }

        [Fact]
        [DisplayName("SetSessionInfo 後 GetSessionInfo 應回傳同一物件，RemoveSessionInfo 後應為 null")]
        public void SessionInfo_SetGetRemove_BehavesCorrectly()
        {
            var token = Guid.NewGuid();
            var info = new SessionInfo
            {
                AccessToken = token,
                UserId = "u1",
                UserName = "User One"
            };

            CacheFunc.SetSessionInfo(info);
            var fromCache = CacheFunc.GetSessionInfo(token);
            Assert.NotNull(fromCache);
            Assert.Equal(token, fromCache!.AccessToken);

            CacheFunc.RemoveSessionInfo(token);
            Assert.Null(CacheFunc.GetSessionInfo(token));
        }

        [Fact]
        [DisplayName("GetProgramSettings 於 tests/Define 無 ProgramSettings.xml 應拋 FileNotFoundException")]
        public void GetProgramSettings_NoSettingsFile_ThrowsFileNotFound()
        {
            // tests/Define 下未放 ProgramSettings.xml,ProgramSettingsCache.CreateInstance 會拋 FileNotFoundException;
            // 目的是讓 CacheFunc.GetProgramSettings 的 delegation 這行被執行並覆蓋。
            Assert.Throws<FileNotFoundException>(() => CacheFunc.GetProgramSettings());
        }

        [Fact]
        [DisplayName("GetFormLayout 於未定義 layoutId 應拋 FileNotFoundException")]
        public void GetFormLayout_UnknownLayoutId_ThrowsFileNotFound()
        {
            // 指定不存在的 layoutId,FileDefineStorage.GetFormLayout 會對檔案路徑驗證失敗
            // 並拋 FileNotFoundException;目的是覆蓋 CacheFunc.GetFormLayout 的 delegation。
            Assert.Throws<FileNotFoundException>(() =>
                CacheFunc.GetFormLayout("__non_existent_layout_" + Guid.NewGuid().ToString("N")));
        }
    }
}
