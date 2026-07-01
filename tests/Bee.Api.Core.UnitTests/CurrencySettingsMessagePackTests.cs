using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Definition.Settings;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 驗證 CurrencySettings（系統層幣別主檔）的 MessagePack wire round-trip。CurrencySettings
    /// ship 給 client 供 UI runtime 解析金額位數，其集合須由自訂 FormatterResolver 的
    /// CollectionBaseFormatter&lt;CurrencySettings, CurrencyItem&gt; 處理。
    /// </summary>
    public sealed class CurrencySettingsMessagePackTests
    {
        [Fact]
        [DisplayName("CurrencySettings MessagePack round-trip 應保留所有幣別與位數")]
        public void CurrencySettings_RoundTrip_PreservesItems()
        {
            CurrencySettings original =
            [
                new CurrencyItem("USD", 0.01m, "$", "US Dollar", "840"),
                new CurrencyItem("JPY", 1m, "¥", "Japanese Yen", "392"),
                new CurrencyItem("BHD", 0.001m, "BD", "Bahraini Dinar", "048"),
            ];

            var bytes = MessagePackCodec.Serialize(original);
            Assert.NotNull(bytes);
            Assert.NotEmpty(bytes);

            var restored = MessagePackCodec.Deserialize<CurrencySettings>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(3, restored!.Count);
            Assert.Equal(2, restored.GetDecimals("USD"));
            Assert.Equal(0, restored.GetDecimals("JPY"));
            Assert.Equal(3, restored.GetDecimals("BHD"));
            Assert.Equal("$", restored.Find("USD")!.Symbol);
        }

        [Fact]
        [DisplayName("CurrencySettings 空集合 MessagePack round-trip 應回空集合（非 null）")]
        public void CurrencySettings_Empty_RoundTrip_Succeeds()
        {
            CurrencySettings original = [];

            var bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<CurrencySettings>(bytes);

            Assert.NotNull(restored);
            Assert.Empty(restored!);
        }
    }
}
