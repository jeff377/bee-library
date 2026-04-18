using System.Buffers;
using System.Collections;
using System.ComponentModel;
using Bee.Definition.Serialization;
using MessagePack;

namespace Bee.Definition.UnitTests.Serialization
{
    /// <summary>
    /// SafeTypelessFormatter 型別白名單驗證測試。
    /// </summary>
    public class SafeTypelessFormatterTests
    {
        [Theory]
        [InlineData("System.Int32")]
        [InlineData("System.String")]
        [InlineData("System.Boolean")]
        [InlineData("System.DateTime")]
        [InlineData("System.Guid")]
        [InlineData("System.Byte[]")]
        [InlineData("System.DBNull")]
        [InlineData("System.Data.DataTable")]
        [DisplayName("IsTypeAllowed 內建原生型別應回傳 true")]
        public void IsTypeAllowed_PrimitiveTypes_ReturnsTrue(string fullName)
        {
            // Act & Assert
            Assert.True(SafeTypelessFormatter.IsTypeAllowed(fullName));
        }

        [Theory]
        [InlineData("Bee.Base.SomeClass")]
        [InlineData("Bee.Definition.Foo")]
        [InlineData("Bee.Contracts.Dto")]
        [InlineData("Bee.Api.Core.Something")]
        [InlineData("Bee.Business.Employee")]
        [DisplayName("IsTypeAllowed Bee.* 命名空間型別應回傳 true")]
        public void IsTypeAllowed_BeeNamespaces_ReturnsTrue(string fullName)
        {
            // Act & Assert
            Assert.True(SafeTypelessFormatter.IsTypeAllowed(fullName));
        }

        [Theory]
        [InlineData("System.Diagnostics.Process")]
        [InlineData("System.IO.File")]
        [InlineData("SomeMalicious.Attacker.Type")]
        [DisplayName("IsTypeAllowed 不在白名單的型別應回傳 false")]
        public void IsTypeAllowed_UntrustedTypes_ReturnsFalse(string fullName)
        {
            // Act & Assert
            Assert.False(SafeTypelessFormatter.IsTypeAllowed(fullName));
        }

        [Fact]
        [DisplayName("SafeTypelessFormatter.Instance 應提供單例")]
        public void Instance_IsNotNull()
        {
            // Act & Assert
            Assert.NotNull(SafeTypelessFormatter.Instance);
        }

        [Fact]
        [DisplayName("Deserialize 於 nil payload 應回傳 null")]
        public void Deserialize_NilPayload_ReturnsNull()
        {
            // 直接呼叫 Deserialize 於 nil 位元組序列,覆蓋 TryReadNil 分支 (line 95-96)。
            byte[] bytes = MessagePackSerializer.Typeless.Serialize((object?)null);
            var buffer = new ReadOnlySequence<byte>(bytes);
            var reader = new MessagePackReader(buffer);

            var result = SafeTypelessFormatter.Instance.Deserialize(ref reader, MessagePackSerializerOptions.Standard);

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("Deserialize 非白名單型別經 post-check 應拋 InvalidOperationException")]
        public void Deserialize_DisallowedType_ThrowsInvalidOperation()
        {
            // 以 System.Version(不在白名單)序列化後,直接走 SafeTypelessFormatter.Deserialize;
            // 傳入 MessagePackSerializerOptions.Standard 略過自訂 pre-check,讓 TypelessFormatter
            // 成功反序列化,最後由 ValidateType post-check 阻擋 (line 137-141)。
            var value = new System.Version(1, 2, 3, 4);
            byte[] bytes = MessagePackSerializer.Typeless.Serialize(value);

            var ex = Assert.Throws<InvalidOperationException>(() => DeserializeViaFormatter(bytes));

            Assert.Contains("blocked", ex.Message);
        }

        // 區域函式避免 ref 區域變數跨 lambda 捕捉造成 CS8175
        private static object? DeserializeViaFormatter(byte[] bytes)
        {
            var buffer = new ReadOnlySequence<byte>(bytes);
            var reader = new MessagePackReader(buffer);
            return SafeTypelessFormatter.Instance.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
        }
    }
}
