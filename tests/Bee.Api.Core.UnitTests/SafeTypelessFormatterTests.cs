using Bee.Api.Core.MessagePack;
using Bee.Definition.Collections;

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
            var original = new ParameterCollection();
            original.Add("IntValue", 42);
            original.Add("StringValue", "Hello");
            original.Add("BoolValue", true);
            original.Add("DecimalValue", 99.99m);
            original.Add("DateTimeValue", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            original.Add("NullValue", null);

            var bytes = MessagePackHelper.Serialize(original);
            var restored = MessagePackHelper.Deserialize<ParameterCollection>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(original.Count, restored.Count);
            Assert.Equal(42, restored["IntValue"].Value);
            Assert.Equal("Hello", restored["StringValue"].Value);
            Assert.Equal(true, restored["BoolValue"].Value);
            Assert.Equal(99.99m, restored["DecimalValue"].Value);
            Assert.Null(restored["NullValue"].Value);
        }

        [Fact(DisplayName = "ParameterCollection 允許 Bee 命名空間型別序列化")]
        public void ParameterCollection_AllowedBeeTypes_RoundTrip()
        {
            var inner = new ParameterCollection();
            inner.Add("Nested", "value");

            var original = new ParameterCollection();
            original.Add("Child", inner);

            var bytes = MessagePackHelper.Serialize(original);
            var restored = MessagePackHelper.Deserialize<ParameterCollection>(bytes);

            Assert.NotNull(restored);
            var restoredChild = restored["Child"].Value as ParameterCollection;
            Assert.NotNull(restoredChild);
            Assert.Equal("value", restoredChild["Nested"].Value);
        }
    }
}
