using System.Buffers;
using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Definition.Collections;
using Bee.Definition.Filters;
using MessagePack;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// <c>SafeTypelessFormatter</c> 的白名單與 round-trip 測試。
    /// </summary>
    /// <remarks>
    /// 此測試原本一分為二（`Bee.Definition.UnitTests` 測白名單、`Bee.Api.Core.UnitTests`
    /// 測 round-trip），因 formatter 遷入 `Bee.Api.Core` 而合併於此。
    /// </remarks>
    public class SafeTypelessFormatterTests
    {
        [Fact(DisplayName = "ParameterCollection 允許安全的基礎型別序列化")]
        public void ParameterCollection_AllowedPrimitiveTypes_RoundTrip()
        {
            var original = new ParameterCollection
            {
                { "IntValue", 42 },
                { "StringValue", "Hello" },
                { "BoolValue", true },
                { "DecimalValue", 99.99m },
                { "DateTimeValue", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                { "NullValue", null! }
            };

            var bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<ParameterCollection>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(original.Count, restored.Count);
            Assert.Equal(42, restored["IntValue"].Value);
            Assert.Equal("Hello", restored["StringValue"].Value);
            Assert.True((bool)restored["BoolValue"].Value!);
            Assert.Equal(99.99m, restored["DecimalValue"].Value);
            Assert.Null(restored["NullValue"].Value);
        }

        [Fact(DisplayName = "ParameterCollection 允許 Bee 命名空間型別序列化")]
        public void ParameterCollection_AllowedBeeTypes_RoundTrip()
        {
            var inner = new ParameterCollection
            {
                { "Nested", "value" }
            };

            var original = new ParameterCollection
            {
                { "Child", inner }
            };

            var bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<ParameterCollection>(bytes);

            Assert.NotNull(restored);
            var restoredChild = restored["Child"].Value as ParameterCollection;
            Assert.NotNull(restoredChild);
            Assert.Equal("value", restoredChild["Nested"].Value);
        }

        [Fact(DisplayName = "ParameterCollection 允許 DateOnly 序列化（日曆日語意的 filter 值）")]
        public void ParameterCollection_DateOnly_RoundTrip()
        {
            var original = new ParameterCollection
            {
                { "DateOnlyValue", new DateOnly(2026, 7, 25) }
            };

            var bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<ParameterCollection>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(new DateOnly(2026, 7, 25), restored["DateOnlyValue"].Value);
        }

        [Fact(DisplayName = "FilterCondition 的 DateOnly 條件值應可 round-trip")]
        public void FilterCondition_DateOnlyValue_RoundTrip()
        {
            var original = FilterCondition.Equal("hire_date", new DateOnly(2026, 7, 25));

            var bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<FilterCondition>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(new DateOnly(2026, 7, 25), restored.Value);
        }

        [Theory]
        [InlineData("System.Int32")]
        [InlineData("System.String")]
        [InlineData("System.Boolean")]
        [InlineData("System.Decimal")]
        [InlineData("System.DateTime")]
        [InlineData("System.DateOnly")]
        [InlineData("System.Guid")]
        [InlineData("System.Byte[]")]
        [InlineData("System.DBNull")]
        [InlineData("System.Data.DataTable")]
        [InlineData("Bee.Base.SomeClass")]
        [InlineData("Bee.Definition.Collections.Parameter")]
        [InlineData("Bee.Api.Contracts.SomeDto")]
        [InlineData("Bee.Api.Core.Something")]
        [InlineData("Bee.Business.Employee")]
        [DisplayName("IsTypeAllowed 應允許原始型別與白名單命名空間")]
        public void IsTypeAllowed_AllowedTypes_ReturnsTrue(string fullName)
        {
            Assert.True(SafeTypelessFormatter.IsTypeAllowed(fullName));
        }

        [Theory]
        [InlineData("System.Diagnostics.Process")]
        [InlineData("System.IO.File")]
        [InlineData("System.IO.FileInfo")]
        [InlineData("System.Runtime.Serialization.Formatters.Binary.BinaryFormatter")]
        [InlineData("Evil.Namespace.Exploit")]
        [InlineData("SomeMalicious.Attacker.Type")]
        [InlineData("System.Data.DataRow")]
        [DisplayName("IsTypeAllowed 應拒絕不在白名單的型別")]
        public void IsTypeAllowed_DisallowedTypes_ReturnsFalse(string fullName)
        {
            Assert.False(SafeTypelessFormatter.IsTypeAllowed(fullName));
        }

        [Fact]
        [DisplayName("SafeTypelessFormatter.Instance 應提供單例")]
        public void Instance_IsNotNull()
        {
            Assert.NotNull(SafeTypelessFormatter.Instance);
        }

        [Fact]
        [DisplayName("Deserialize 於 nil payload 應回傳 null")]
        public void Deserialize_NilPayload_ReturnsNull()
        {
            // 直接對 nil 位元組序列呼叫，覆蓋 TryReadNil 分支。
            var bytes = MessagePackSerializer.Typeless.Serialize((object?)null);

            Assert.Null(DeserializeViaFormatter(bytes));
        }

        [Fact]
        [DisplayName("Deserialize 非白名單型別經 post-check 應拋 InvalidOperationException")]
        public void Deserialize_DisallowedType_ThrowsInvalidOperation()
        {
            // 傳入 MessagePackSerializerOptions.Standard 會略過自訂的 pre-check，
            // 讓 TypelessFormatter 順利建出物件，改由 ValidateType 的 post-check 擋下 ——
            // 這條路徑驗證的正是「防禦兩層」的第二層。
            var bytes = MessagePackSerializer.Typeless.Serialize(new Version(1, 2, 3, 4));

            var exception = Assert.Throws<InvalidOperationException>(
                () => DeserializeViaFormatter(bytes));

            Assert.Contains("blocked", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// 區域方法而非 lambda：`ref` 區域變數無法被 lambda 捕捉（CS8175）。
        /// </summary>
        private static object? DeserializeViaFormatter(byte[] bytes)
        {
            var reader = new MessagePackReader(new ReadOnlySequence<byte>(bytes));
            return SafeTypelessFormatter.Instance.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
        }
    }
}
