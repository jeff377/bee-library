using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Definition.Collections;
using Bee.Definition.Forms;
using Bee.Definition.Sorting;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// SPIKE（階段 0 待決 A）：驗證 `Parameter.Value` 的
    /// `[MessagePackFormatter(typeof(SafeTypelessFormatter))]` 是否為必要保險。
    /// 通過條件：移除 attribute 後，經 `MessagePackCodec` 的 round-trip 仍會擋下白名單外型別。
    /// </summary>
    public class SpikeParameterValueTests
    {
        [Fact]
        [DisplayName("SPIKE: Parameter.Value 帶白名單內型別應可 round-trip")]
        public void ParameterValue_AllowedType_RoundTrips()
        {
            var source = new ParameterCollection
            {
                new Parameter("s", "hello"),
                new Parameter("i", 42),
                new Parameter("g", Guid.NewGuid()),
            };

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<ParameterCollection>(bytes);

            Assert.Equal("hello", result["s"].Value);
            Assert.Equal(42, result["i"].Value);
            Assert.Equal(source["g"].Value, result["g"].Value);
        }

        [Fact]
        [DisplayName("SPIKE: Parameter.Value 帶白名單外型別，反序列化必須被擋下")]
        public void ParameterValue_DisallowedType_IsBlocked()
        {
            var source = new ParameterCollection
            {
                new Parameter("evil", new Version(1, 2, 3, 4)),
            };

            var bytes = MessagePackCodec.Serialize(source);

            // 白名單外型別（System.Version）必須在反序列化被擋，否則就是 gadget 破口。
            var exception = Record.Exception(
                () => MessagePackCodec.Deserialize<ParameterCollection>(bytes));

            Assert.NotNull(exception);
        }
        [Fact]
        [DisplayName("SPIKE: typeless 通道帶「已註冊」的集合型別 (SortFieldCollection)")]
        public void ParameterValue_RegisteredCollection_RoundTrips()
        {
            var coll = new SortFieldCollection { new SortField { FieldName = "a" } };
            var source = new ParameterCollection { new Parameter("c", coll) };

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<ParameterCollection>(bytes);

            var restored = Assert.IsType<SortFieldCollection>(result["c"].Value);
            Assert.Single(restored);
            Assert.Equal("a", restored[0].FieldName);
        }

        [Fact]
        [DisplayName("SPIKE: typeless 通道帶「未註冊」的集合型別 (FormFieldCollection)")]
        public void ParameterValue_UnregisteredCollection_Behaviour()
        {
            var coll = new FormFieldCollection { new FormField { FieldName = "a" } };
            var source = new ParameterCollection { new Parameter("c", coll) };

            var serializeEx = Record.Exception(() => MessagePackCodec.Serialize(source));
            Assert.Null(serializeEx);

            var bytes = MessagePackCodec.Serialize(source);
            var deserializeEx = Record.Exception(
                () => MessagePackCodec.Deserialize<ParameterCollection>(bytes));

            Assert.True(deserializeEx == null,
                $"未註冊集合反序列化失敗：{deserializeEx?.GetType().Name}: {deserializeEx?.Message}");

            // 關鍵：不擲例外 != 內容正確。靜默掉內容比擲例外更糟。
            var result = MessagePackCodec.Deserialize<ParameterCollection>(bytes);
            var restored = Assert.IsType<FormFieldCollection>(result["c"].Value);
            Assert.Single(restored);
            Assert.Equal("a", restored[0].FieldName);
        }
        [Fact]
        [DisplayName("SPIKE: typeless 通道帶「未註冊的 MessagePackCollectionBase 子型別」")]
        public void ParameterValue_UnregisteredMessagePackCollection_Behaviour()
        {
            var coll = new SpikeUnregisteredCollection { new SpikeItem { Name = "a" } };
            var source = new ParameterCollection { new Parameter("c", coll) };

            var bytes = MessagePackCodec.Serialize(source);
            var ex = Record.Exception(
                () => MessagePackCodec.Deserialize<ParameterCollection>(bytes));

            Assert.True(ex == null,
                $"未註冊的 MessagePackCollectionBase 子型別反序列化失敗："
                + $"{ex?.GetType().Name}: {ex?.Message}");
        }

        [Fact]
        [DisplayName("SPIKE: 無標註集合的 wire 格式應與 CollectionBaseFormatter 一致（array）")]
        public void UnattributedCollection_ProducesArrayWireShape()
        {
            var registered = new SortFieldCollection { new SortField { FieldName = "a" } };
            var unregistered = new SpikeUnregisteredCollection { new SpikeItem { Name = "a" } };

            var regBytes = MessagePackCodec.Serialize(registered);
            var unregBytes = MessagePackCodec.Serialize(unregistered);

            // MessagePack fixarray 的首位元組為 0x90|n；fixmap 為 0x80|n。
            // 兩者皆應為 array（0x91 = 1 個元素的陣列）。
            Assert.Equal(0x91, regBytes[0]);
            Assert.Equal(0x91, unregBytes[0]);
        }

        [Fact]
        [DisplayName("SPIKE: 無標註的 item 型別，contractless 會納入哪些成員？")]
        public void UnattributedItem_ContractlessMemberSet()
        {
            var bytes = MessagePackCodec.Serialize(new SpikeItem { Name = "a" });

            // fixmap 首位元組 0x80|n。SpikeItem 自有 Name，基底另有
            // Tag(get/set) / SerializeState(private set) / Collection(get only)。
            var memberCount = bytes[0] & 0x0F;
            Assert.True(bytes[0] >= 0x80 && bytes[0] <= 0x8F,
                $"預期 fixmap，實得 0x{bytes[0]:X2}");
            Assert.True(memberCount == 1,
                $"contractless 納入 {memberCount} 個成員（期望只有 Name）。"
                + $"payload={BitConverter.ToString(bytes)}");
        }
    }

    /// <summary>SPIKE 用：刻意不在 MessagePackCodec 註冊的集合型別。</summary>
    public class SpikeUnregisteredCollection : MessagePackCollectionBase<SpikeItem> { }

    /// <summary>SPIKE 用集合項目。</summary>
    public class SpikeItem : MessagePackCollectionItem
    {
        /// <summary>名稱。</summary>
        public string Name { get; set; } = string.Empty;
    }
}
