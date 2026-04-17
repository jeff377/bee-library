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
    }
}
