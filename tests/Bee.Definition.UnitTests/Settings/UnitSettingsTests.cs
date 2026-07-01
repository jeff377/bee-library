using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// UnitSettings（系統層計量單位主檔）：GetDecimals 命中/fallback，以及三棲（XML / JSON）round-trip。
    /// </summary>
    public class UnitSettingsTests
    {
        private static UnitSettings BuildSettings() =>
        [
            new UnitItem("KG", 3, "weight", "Kilogram"),
            new UnitItem("PCS", 0, "count", "Pieces"),
            new UnitItem("M", 2, "length", "Metre"),
        ];

        [Theory]
        [InlineData("KG", 3)]
        [InlineData("PCS", 0)]
        [InlineData("M", 2)]
        [DisplayName("GetDecimals 命中回該單位位數")]
        public void GetDecimals_Hit_ReturnsUnitDecimals(string code, int expected)
        {
            var settings = BuildSettings();

            Assert.Equal(expected, settings.GetDecimals(code));
        }

        [Fact]
        [DisplayName("GetDecimals 未命中回 fallback 0")]
        public void GetDecimals_Miss_ReturnsFallback()
        {
            var settings = BuildSettings();

            Assert.Equal(0, settings.GetDecimals("XXX"));
            Assert.Equal(0, settings.GetDecimals(""));
        }

        [Fact]
        [DisplayName("GetDecimals 單位碼比對不分大小寫")]
        public void GetDecimals_CaseInsensitive()
        {
            var settings = BuildSettings();

            Assert.Equal(3, settings.GetDecimals("kg"));
        }

        [Fact]
        [DisplayName("Find 命中回項目、未命中回 null")]
        public void Find_HitAndMiss()
        {
            var settings = BuildSettings();

            Assert.Equal("Kilogram", settings.Find("KG")?.Name);
            Assert.Null(settings.Find("XXX"));
        }

        [Fact]
        [DisplayName("UnitSettings XML 序列化應正確還原所有欄位")]
        public void UnitSettings_XmlRoundtrip_PreservesItems()
        {
            var original = BuildSettings();

            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<UnitSettings>(xml);

            Assert.NotNull(restored);
            Assert.Equal(3, restored!.Count);
            var kg = restored.Find("KG");
            Assert.NotNull(kg);
            Assert.Equal(3, kg!.Decimals);
            Assert.Equal("weight", kg.Dimension);
            Assert.Equal("Kilogram", kg.Name);
            Assert.Equal(0, restored.GetDecimals("PCS"));
        }

        [Fact]
        [DisplayName("UnitSettings JSON 序列化應正確還原所有欄位")]
        public void UnitSettings_JsonRoundtrip_PreservesItems()
        {
            var original = BuildSettings();

            var json = JsonCodec.Serialize(original);
            var restored = JsonCodec.Deserialize<UnitSettings>(json);

            Assert.NotNull(restored);
            Assert.Equal(3, restored!.Count);
            Assert.Equal(3, restored.GetDecimals("KG"));
            Assert.Equal(0, restored.GetDecimals("PCS"));
        }
    }
}
