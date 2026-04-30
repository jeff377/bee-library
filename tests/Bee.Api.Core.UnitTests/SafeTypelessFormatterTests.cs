using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Definition.Collections;
using Bee.Definition.Serialization;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// SafeTypelessFormatter 安全性測試
    /// </summary>
    [Collection("Initialize")]
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

        [Theory]
        [InlineData("System.Int32", true)]
        [InlineData("System.String", true)]
        [InlineData("System.Boolean", true)]
        [InlineData("System.Decimal", true)]
        [InlineData("System.DateTime", true)]
        [InlineData("System.Guid", true)]
        [InlineData("System.Byte[]", true)]
        [InlineData("System.DBNull", true)]
        [InlineData("Bee.Base.SomeClass", true)]
        [InlineData("Bee.Definition.Collections.Parameter", true)]
        [InlineData("Bee.Contracts.SomeDto", true)]
        [DisplayName("IsTypeAllowed 應允許原始型別與白名單命名空間")]
        public void IsTypeAllowed_AllowedTypes_ReturnsTrue(string fullName, bool expected)
        {
            Assert.Equal(expected, SafeTypelessFormatter.IsTypeAllowed(fullName));
        }

        [Theory]
        [InlineData("System.Diagnostics.Process")]
        [InlineData("System.IO.FileInfo")]
        [InlineData("System.Runtime.Serialization.Formatters.Binary.BinaryFormatter")]
        [InlineData("Evil.Namespace.Exploit")]
        [InlineData("System.Data.DataRow")]
        [DisplayName("IsTypeAllowed 應拒絕不在白名單的型別")]
        public void IsTypeAllowed_DisallowedTypes_ReturnsFalse(string fullName)
        {
            Assert.False(SafeTypelessFormatter.IsTypeAllowed(fullName));
        }
    }
}
