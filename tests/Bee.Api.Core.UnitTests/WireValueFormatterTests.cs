using System.Buffers;
using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Definition.Collections;
using Bee.Definition.Filters;
using Bee.Tests.Shared;
using MessagePack;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// <c>WireValueFormatter</c> 的白名單與 round-trip 測試。
    /// </summary>
    /// <remarks>
    /// 前身為 <c>SafeTypelessFormatterTests</c>。formatter 由 <c>TypelessFormatter</c> 包裝改為
    /// 判別式封閉集合後，白名單語意不變，改變的是「未知型別」那條路徑的框架格式。
    /// </remarks>
    public class WireValueFormatterTests
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

        [Theory]
        [InlineData((sbyte)-8)]
        [InlineData((byte)8)]
        [InlineData((short)-16)]
        [InlineData((ushort)16)]
        [InlineData(-32)]
        [InlineData(32u)]
        [InlineData(-64L)]
        [InlineData(64UL)]
        [InlineData(1.5f)]
        [InlineData(2.5d)]
        [DisplayName("整數與浮點各寬度皆應 round-trip 回原型別")]
        public void ParameterValue_NumericWidths_RoundTrip(object value)
        {
            var restored = RoundTripValue(value);

            Assert.Equal(value.GetType(), restored!.GetType());
            Assert.Equal(value, restored);
        }

        [Fact]
        [DisplayName("非數值的已知型別皆應 round-trip 回原型別")]
        public void ParameterValue_KnownReferenceAndStructTypes_RoundTrip()
        {
            var guid = Guid.NewGuid();
            var bytes = new byte[] { 1, 2, 3 };

            Assert.Equal(guid, RoundTripValue(guid));
            Assert.Equal(new DateOnly(2026, 7, 25), RoundTripValue(new DateOnly(2026, 7, 25)));
            Assert.Equal(TimeSpan.FromMinutes(90), RoundTripValue(TimeSpan.FromMinutes(90)));
            Assert.Equal(bytes, (byte[])RoundTripValue(bytes)!);
            Assert.Equal(DBNull.Value, RoundTripValue(DBNull.Value));
        }

        [Fact]
        [DisplayName("object[] 條件值（IN 子句）應遞迴 round-trip")]
        public void ParameterValue_ObjectArray_RoundTrip()
        {
            var guid = Guid.NewGuid();
            var restored = (object?[])RoundTripValue(new object[] { 1, "two", guid })!;

            Assert.Equal(3, restored.Length);
            Assert.Equal(1, restored[0]);
            Assert.Equal("two", restored[1]);
            Assert.Equal(guid, restored[2]);
        }

        [DynamicCodeFact(DisplayName = "ParameterCollection 允許 Bee 命名空間型別序列化（具名型別通道，需動態碼）")]
        public void ParameterCollection_AllowedBeeTypes_RoundTrip()
        {
            // 這條走的是 `WireValueFormatter` 的「具名型別」分支：型別不在封閉判別集合內，
            // 只能經非泛型多載遞迴，因此在無動態碼的 runtime（iOS）上不可用——
            // 那是 `SysInfo.AllowedTypeNamespaces` 這個可設定擴充點的固有限制，非缺陷。
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
            Assert.True(WireTypeWhitelist.IsTypeAllowed(fullName));
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
            Assert.False(WireTypeWhitelist.IsTypeAllowed(fullName));
        }

        [Fact]
        [DisplayName("WireValueFormatter.Instance 應提供單例")]
        public void Instance_IsNotNull()
        {
            Assert.NotNull(WireValueFormatter.Instance);
        }

        [Fact]
        [DisplayName("Deserialize 於 nil payload 應回傳 null")]
        public void Deserialize_NilPayload_ReturnsNull()
        {
            var bytes = MessagePackCodec.Serialize<object?>(null);

            Assert.Null(DeserializeViaFormatter(bytes));
        }

        [Fact]
        [DisplayName("Deserialize 非白名單型別應在解析型別前擋下")]
        public void Deserialize_DisallowedType_ThrowsInvalidOperation()
        {
            // 手工組出「具名型別」那條路徑的封套，型別名為白名單外的型別。
            // 攔截點在 `Type.GetType` 之前，故此型別自始不會被載入。
            var bytes = BuildNamedEnvelope(typeof(global::System.Diagnostics.Process).AssemblyQualifiedName!);

            var exception = Assert.Throws<InvalidOperationException>(
                () => DeserializeViaFormatter(bytes));

            Assert.Contains("blocked", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("Deserialize 未知判別碼應拋 MessagePackSerializationException")]
        public void Deserialize_UnknownCode_Throws()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(buffer);
            writer.WriteArrayHeader(2);
            writer.Write(9999);
            writer.WriteNil();
            writer.Flush();

            Assert.Throws<MessagePackSerializationException>(
                () => DeserializeViaFormatter(buffer.WrittenSpan.ToArray()));
        }

        private static byte[] BuildNamedEnvelope(string assemblyQualifiedName)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(buffer);
            writer.WriteArrayHeader(2);
            writer.Write(assemblyQualifiedName);
            writer.WriteNil();
            writer.Flush();
            return buffer.WrittenSpan.ToArray();
        }

        private static object? RoundTripValue(object value)
        {
            var source = new ParameterCollection { { "v", value } };
            var restored = MessagePackCodec.Deserialize<ParameterCollection>(MessagePackCodec.Serialize(source));
            return restored!["v"].Value;
        }

        /// <summary>
        /// 區域方法而非 lambda：`ref` 區域變數無法被 lambda 捕捉（CS8175）。
        /// </summary>
        private static object? DeserializeViaFormatter(byte[] bytes)
        {
            var reader = new MessagePackReader(new ReadOnlySequence<byte>(bytes));
            return WireValueFormatter.Instance.Deserialize(ref reader, MessagePackSerializerOptions.Standard);
        }
    }
}
