using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="LocalDefineAccess"/> 讀取路徑測試。透過 fixture 的 DI 容器解析共用實例
    /// （path = <c>tests/Define</c>），仍保留 <c>[Collection("Initialize")]</c> 序列化—— Get 路徑
    /// 在 cache miss 時走 process-wide <see cref="CacheContainer"/> 與 <see cref="DefinePathInfo"/>
    /// 靜態，可能與 <c>DatabaseSettingsCacheTests</c> 等 mutator 競爭；待 PR 5.7 cache 改注入 PathOptions
    /// 後再脫除 Collection。
    /// </summary>
    [Collection("Initialize")]
    public class LocalDefineAccessTests : IClassFixture<BeeTestFixture>
    {
        private static readonly string[] s_tableSchemaKeys = { "common", "st_user" };
        private static readonly string[] s_formSchemaKeys = { "Department" };

        private readonly IDefineAccess _access;

        public LocalDefineAccessTests(BeeTestFixture fx)
        {
            _access = fx.GetRequiredService<IDefineAccess>();
        }

        [Fact]
        [DisplayName("GetDefine(SystemSettings) 應回傳 SystemSettings 實例")]
        public void GetDefine_SystemSettings_ReturnsSystemSettings()
        {
            var result = _access.GetDefine(DefineType.SystemSettings);
            Assert.IsType<SystemSettings>(result);
        }

        [Fact]
        [DisplayName("GetDefine(DatabaseSettings) 應回傳 DatabaseSettings 實例")]
        public void GetDefine_DatabaseSettings_ReturnsDatabaseSettings()
        {
            var result = _access.GetDefine(DefineType.DatabaseSettings);
            Assert.IsType<DatabaseSettings>(result);
        }

        [Fact]
        [DisplayName("GetDefine(DbCategorySettings) 應回傳 DbCategorySettings 實例")]
        public void GetDefine_DbCategorySettings_ReturnsDbCategorySettings()
        {
            var result = _access.GetDefine(DefineType.DbCategorySettings);
            Assert.IsType<DbCategorySettings>(result);
        }

        [Fact]
        [DisplayName("GetDefine(TableSchema) 帶兩個 keys 應回傳對應 TableSchema")]
        public void GetDefine_TableSchema_WithCorrectKeys_ReturnsTableSchema()
        {
            var result = _access.GetDefine(DefineType.TableSchema, s_tableSchemaKeys);
            var schema = Assert.IsType<TableSchema>(result);
            Assert.Equal("st_user", schema.TableName, ignoreCase: true);
        }

        [Fact]
        [DisplayName("GetDefine(FormSchema) 帶單一 key 應回傳對應 FormSchema")]
        public void GetDefine_FormSchema_WithCorrectKey_ReturnsFormSchema()
        {
            var result = _access.GetDefine(DefineType.FormSchema, s_formSchemaKeys);
            var schema = Assert.IsType<FormSchema>(result);
            Assert.Equal("Department", schema.ProgId);
        }

        [Theory]
        [InlineData(DefineType.TableSchema, null)]
        [InlineData(DefineType.TableSchema, new string[] { "only-one" })]
        [InlineData(DefineType.FormSchema, null)]
        [InlineData(DefineType.FormSchema, new string[] { "a", "b" })]
        [InlineData(DefineType.FormLayout, null)]
        [DisplayName("GetDefine 對 keys 數量不符的型別應拋 ArgumentException")]
        public void GetDefine_InvalidKeys_Throws(DefineType defineType, string[]? keys)
        {
            Assert.Throws<ArgumentException>(() => _access.GetDefine(defineType, keys));
        }

        [Fact]
        [DisplayName("GetDefine 對未支援的 DefineType 應拋 NotSupportedException")]
        public void GetDefine_UnsupportedType_Throws()
        {
            Assert.Throws<NotSupportedException>(() => _access.GetDefine((DefineType)999));
        }

        [Fact]
        [DisplayName("SaveDefine 對未支援的 DefineType 應拋 NotSupportedException")]
        public void SaveDefine_UnsupportedType_Throws()
        {
            Assert.Throws<NotSupportedException>(() =>
                _access.SaveDefine((DefineType)999, new object()));
        }

        [Fact]
        [DisplayName("SaveDefine(TableSchema) 帶錯誤 keys 應拋 ArgumentException")]
        public void SaveDefine_TableSchema_InvalidKeys_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                _access.SaveDefine(DefineType.TableSchema, new TableSchema(), null));
        }

        [Fact]
        [DisplayName("GetSystemSettings 應回傳實例")]
        public void GetSystemSettings_ReturnsInstance() => Assert.NotNull(_access.GetSystemSettings());

        [Fact]
        [DisplayName("GetDatabaseSettings 應回傳實例")]
        public void GetDatabaseSettings_ReturnsInstance() => Assert.NotNull(_access.GetDatabaseSettings());

        [Fact]
        [DisplayName("GetDbCategorySettings 應回傳實例")]
        public void GetDbCategorySettings_ReturnsInstance() => Assert.NotNull(_access.GetDbCategorySettings());

        [Fact]
        [DisplayName("GetTableSchema 應回傳實例")]
        public void GetTableSchema_ReturnsInstance()
        {
            var schema = _access.GetTableSchema("common", "st_user");
            Assert.NotNull(schema);
            Assert.Equal("st_user", schema.TableName, ignoreCase: true);
        }

        [Fact]
        [DisplayName("GetFormSchema 應回傳實例")]
        public void GetFormSchema_ReturnsInstance()
        {
            var schema = _access.GetFormSchema("Employee");
            Assert.NotNull(schema);
            Assert.Equal("Employee", schema.ProgId);
        }
    }
}
