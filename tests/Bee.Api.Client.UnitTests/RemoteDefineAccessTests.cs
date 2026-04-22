using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.DefineAccess;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Tests.Shared;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="RemoteDefineAccess"/> 參數防護與不支援型別的純邏輯測試。
    /// 以 local <see cref="SystemApiConnector"/> 建構，但測試案例僅涵蓋在呼叫實際遠端之前就應拋出的錯誤路徑。
    /// </summary>
    [Collection("Initialize")]
    public class RemoteDefineAccessTests
    {
        private static readonly string[] s_singleKey = { "onlyOne" };
        private static readonly string[] s_twoKeys = { "a", "b" };

        private static RemoteDefineAccess CreateAccess()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            return new RemoteDefineAccess(connector);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine TableSchema keys 為 null 應拋 ArgumentException")]
        public void GetDefine_TableSchema_NullKeys_ThrowsArgumentException()
        {
            var access = CreateAccess();
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.TableSchema, null));
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine TableSchema keys 長度不足應拋 ArgumentException")]
        public void GetDefine_TableSchema_InsufficientKeys_ThrowsArgumentException()
        {
            var access = CreateAccess();
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.TableSchema, Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.TableSchema, s_singleKey));
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine FormSchema keys 為 null 應拋 ArgumentException")]
        public void GetDefine_FormSchema_NullKeys_ThrowsArgumentException()
        {
            var access = CreateAccess();
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.FormSchema, null));
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine FormSchema keys 長度不為 1 應拋 ArgumentException")]
        public void GetDefine_FormSchema_InvalidKeysLength_ThrowsArgumentException()
        {
            var access = CreateAccess();
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.FormSchema, Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.FormSchema, s_twoKeys));
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine FormLayout keys 為 null 應拋 ArgumentException")]
        public void GetDefine_FormLayout_NullKeys_ThrowsArgumentException()
        {
            var access = CreateAccess();
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.FormLayout, null));
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine FormLayout keys 長度不為 1 應拋 ArgumentException")]
        public void GetDefine_FormLayout_InvalidKeysLength_ThrowsArgumentException()
        {
            var access = CreateAccess();
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.FormLayout, Array.Empty<string>()));
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.FormLayout, s_twoKeys));
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine 不支援的 DefineType 應拋 NotSupportedException")]
        public void GetDefine_UnsupportedDefineType_ThrowsNotSupportedException()
        {
            var access = CreateAccess();
            Assert.Throws<NotSupportedException>(() => access.GetDefine((DefineType)999));
        }

        [Fact]
        [DisplayName("RemoteDefineAccess 建構子以 SystemApiConnector 建立時不應拋例外")]
        public void Constructor_WithConnector_DoesNotThrow()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);
            Assert.NotNull(access);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetSystemSettings 本機連線應回傳系統設定")]
        public void GetSystemSettings_LocalConnector_ReturnsSettings()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);

            var settings = access.GetSystemSettings();

            Assert.NotNull(settings);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDatabaseSettings 本機連線應回傳資料庫設定")]
        public void GetDatabaseSettings_LocalConnector_ReturnsSettings()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);

            var settings = access.GetDatabaseSettings();

            Assert.NotNull(settings);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDbSchemaSettings 本機連線應回傳資料庫綱要設定")]
        public void GetDbSchemaSettings_LocalConnector_ReturnsSettings()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);

            var settings = access.GetDbSchemaSettings();

            Assert.NotNull(settings);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetFormSchema 本機連線應回傳表單結構定義")]
        public void GetFormSchema_LocalConnector_ReturnsFormSchema()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);

            var schema = access.GetFormSchema("Employee");

            Assert.NotNull(schema);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetTableSchema 本機連線應回傳資料表結構定義")]
        public void GetTableSchema_LocalConnector_ReturnsTableSchema()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);

            var schema = access.GetTableSchema("common", "st_user");

            Assert.NotNull(schema);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine 重複呼叫應使用快取回傳相同物件")]
        public void GetDefine_SystemSettings_SecondCall_ReturnsCachedObject()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);

            var result1 = access.GetSystemSettings();
            var result2 = access.GetSystemSettings();

            Assert.NotNull(result1);
            Assert.Same(result1, result2);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine SystemSettings 使用 GetDefine 公開方法應回傳系統設定")]
        public void GetDefine_SystemSettings_ViaPublicMethod_ReturnsSettings()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);

            var result = access.GetDefine(DefineType.SystemSettings);

            Assert.NotNull(result);
            Assert.IsType<SystemSettings>(result);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine DatabaseSettings 使用 GetDefine 公開方法應回傳資料庫設定")]
        public void GetDefine_DatabaseSettings_ViaPublicMethod_ReturnsSettings()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);

            var result = access.GetDefine(DefineType.DatabaseSettings);

            Assert.NotNull(result);
            Assert.IsType<DatabaseSettings>(result);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine FormSchema 含有效 key 使用 GetDefine 公開方法應回傳表單定義")]
        public void GetDefine_FormSchema_ViaPublicMethod_ReturnsFormSchema()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);

            var result = access.GetDefine(DefineType.FormSchema, new[] { "Employee" });

            Assert.NotNull(result);
            Assert.IsType<FormSchema>(result);
        }

        [Fact]
        [DisplayName("RemoteDefineAccess.GetDefine TableSchema 含有效 keys 使用 GetDefine 公開方法應回傳資料表定義")]
        public void GetDefine_TableSchema_ViaPublicMethod_ReturnsTableSchema()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var access = new RemoteDefineAccess(connector);

            var result = access.GetDefine(DefineType.TableSchema, new[] { "common", "st_user" });

            Assert.NotNull(result);
            Assert.IsType<TableSchema>(result);
        }
    }
}
