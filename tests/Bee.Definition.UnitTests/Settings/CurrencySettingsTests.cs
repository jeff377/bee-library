using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// CurrencySettings（系統層幣別主檔）：GetRounding / GetDecimals（因子反推）/ fallback，
    /// 以及三棲（XML / JSON）round-trip。
    /// </summary>
    public class CurrencySettingsTests
    {
        private static CurrencySettings BuildSettings() =>
        [
            new CurrencyItem("USD", 0.01m, "$", "US Dollar", "840"),
            new CurrencyItem("JPY", 1m, "¥", "Japanese Yen", "392"),
            new CurrencyItem("BHD", 0.001m, "BD", "Bahraini Dinar", "048"),
        ];

        [Fact]
        [DisplayName("GetRounding 命中應回該幣別自然最小單位")]
        public void GetRounding_Hit_ReturnsRounding()
        {
            var settings = BuildSettings();

            Assert.Equal(0.01m, settings.GetRounding("USD"));
            Assert.Equal(1m, settings.GetRounding("JPY"));
            Assert.Equal(0.001m, settings.GetRounding("BHD"));
        }

        [Fact]
        [DisplayName("GetRounding 未命中應退 fallback 0.01")]
        public void GetRounding_Miss_ReturnsFallback()
        {
            var settings = BuildSettings();

            Assert.Equal(0.01m, settings.GetRounding("XXX"));
            Assert.Equal(0.01m, settings.GetRounding(""));
        }

        [Fact]
        [DisplayName("GetRounding 幣別碼比對應不分大小寫")]
        public void GetRounding_CaseInsensitive()
        {
            var settings = BuildSettings();

            Assert.Equal(1m, settings.GetRounding("jpy"));
        }

        [Theory]
        [InlineData("USD", 2)]
        [InlineData("JPY", 0)]
        [InlineData("BHD", 3)]
        [InlineData("XXX", 2)] // fallback 0.01 → 2
        [DisplayName("GetDecimals 由因子反推顯示位數")]
        public void GetDecimals_DerivesFromRounding(string code, int expected)
        {
            var settings = BuildSettings();

            Assert.Equal(expected, settings.GetDecimals(code));
        }

        [Theory]
        [InlineData("0.01", 2)]
        [InlineData("0.001", 3)]
        [InlineData("1", 0)]
        [InlineData("10", 0)]
        [InlineData("0.05", 2)]
        [InlineData("0", 0)]
        [DisplayName("DecimalsFromRounding 由因子計算位數（decimal-safe）")]
        public void DecimalsFromRounding_ComputesDecimals(string roundingText, int expected)
        {
            var rounding = decimal.Parse(roundingText, System.Globalization.CultureInfo.InvariantCulture);

            Assert.Equal(expected, CurrencySettings.DecimalsFromRounding(rounding));
        }

        [Fact]
        [DisplayName("Find 命中回項目、未命中回 null")]
        public void Find_HitAndMiss()
        {
            var settings = BuildSettings();

            Assert.Equal("US Dollar", settings.Find("USD")?.Name);
            Assert.Null(settings.Find("XXX"));
        }

        [Fact]
        [DisplayName("CurrencySettings XML 序列化應正確還原所有欄位")]
        public void CurrencySettings_XmlRoundtrip_PreservesItems()
        {
            var original = BuildSettings();

            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<CurrencySettings>(xml);

            Assert.NotNull(restored);
            Assert.Equal(3, restored!.Count);
            var usd = restored.Find("USD");
            Assert.NotNull(usd);
            Assert.Equal("840", usd!.Numeric);
            Assert.Equal(0.01m, usd.Rounding);
            Assert.Equal("$", usd.Symbol);
            Assert.Equal("US Dollar", usd.Name);
            Assert.Equal(0, restored.GetDecimals("JPY"));
            Assert.Equal(3, restored.GetDecimals("BHD"));
        }

        [Fact]
        [DisplayName("CurrencySettings JSON 序列化應正確還原所有欄位")]
        public void CurrencySettings_JsonRoundtrip_PreservesItems()
        {
            var original = BuildSettings();

            var json = JsonCodec.Serialize(original);
            var restored = JsonCodec.Deserialize<CurrencySettings>(json);

            Assert.NotNull(restored);
            Assert.Equal(3, restored!.Count);
            Assert.Equal(2, restored.GetDecimals("USD"));
            Assert.Equal(0, restored.GetDecimals("JPY"));
        }
    }
}
