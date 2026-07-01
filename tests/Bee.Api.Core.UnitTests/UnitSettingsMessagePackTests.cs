using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Definition.Settings;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 驗證 UnitSettings（系統層計量單位主檔）的 MessagePack wire round-trip。UnitSettings ship 給
    /// client 供 UI runtime 解析數量/重量位數，其集合須由自訂 FormatterResolver 的
    /// CollectionBaseFormatter&lt;UnitSettings, UnitItem&gt; 處理。
    /// </summary>
    public sealed class UnitSettingsMessagePackTests
    {
        [Fact]
        [DisplayName("UnitSettings MessagePack round-trip 應保留所有單位與位數")]
        public void UnitSettings_RoundTrip_PreservesItems()
        {
            UnitSettings original =
            [
                new UnitItem("KG", 3, "weight", "Kilogram"),
                new UnitItem("PCS", 0, "count", "Pieces"),
            ];

            var bytes = MessagePackCodec.Serialize(original);
            Assert.NotNull(bytes);
            Assert.NotEmpty(bytes);

            var restored = MessagePackCodec.Deserialize<UnitSettings>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(2, restored!.Count);
            Assert.Equal(3, restored.GetDecimals("KG"));
            Assert.Equal(0, restored.GetDecimals("PCS"));
            Assert.Equal("weight", restored.Find("KG")!.Dimension);
        }

        [Fact]
        [DisplayName("UnitSettings 空集合 MessagePack round-trip 應回空集合（非 null）")]
        public void UnitSettings_Empty_RoundTrip_Succeeds()
        {
            UnitSettings original = [];

            var bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<UnitSettings>(bytes);

            Assert.NotNull(restored);
            Assert.Empty(restored!);
        }
    }
}
