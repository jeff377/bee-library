using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.UnitTests
{
    [Collection("Initialize")]
    public class LocalDefineAccessTests
    {
        private readonly LocalDefineAccess _access = new LocalDefineAccess();

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
        [DisplayName("GetDefine(DbSchemaSettings) 應回傳 DbSchemaSettings 實例")]
        public void GetDefine_DbSchemaSettings_ReturnsDbSchemaSettings()
        {
            var result = _access.GetDefine(DefineType.DbSchemaSettings);
            Assert.IsType<DbSchemaSettings>(result);
        }

        [Fact]
        [DisplayName("GetDefine(TableSchema) 帶兩個 keys 應回傳對應 TableSchema")]
        public void GetDefine_TableSchema_WithCorrectKeys_ReturnsTableSchema()
        {
            var result = _access.GetDefine(DefineType.TableSchema, new[] { "common", "st_user" });
            var schema = Assert.IsType<TableSchema>(result);
            Assert.Equal("st_user", schema.TableName, ignoreCase: true);
        }

        [Fact]
        [DisplayName("GetDefine(FormSchema) 帶單一 key 應回傳對應 FormSchema")]
        public void GetDefine_FormSchema_WithCorrectKey_ReturnsFormSchema()
        {
            var result = _access.GetDefine(DefineType.FormSchema, new[] { "Department" });
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
        [DisplayName("GetDbSchemaSettings 應回傳實例")]
        public void GetDbSchemaSettings_ReturnsInstance() => Assert.NotNull(_access.GetDbSchemaSettings());

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
