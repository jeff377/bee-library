using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.DefineAccess;
using Bee.Definition;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="RemoteDefineAccess"/> 參數防護與不支援型別的純邏輯測試。
    /// 以 local <see cref="SystemApiConnector"/> 建構，但測試案例僅涵蓋在呼叫實際遠端之前就應拋出的錯誤路徑。
    /// </summary>
    public class RemoteDefineAccessTests
    {
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
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.TableSchema, new[] { "onlyOne" }));
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
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.FormSchema, new[] { "a", "b" }));
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
            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.FormLayout, new[] { "a", "b" }));
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
    }
}
