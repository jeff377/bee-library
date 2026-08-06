using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.System;

namespace Bee.Api.Core.UnitTests.System
{
    /// <summary>
    /// plugin 維護 API 的 wire 層 round-trip。兩個方向都以 XML 字串承載綁定，
    /// 而不是 <c>PluginSettings</c> 物件——那些是 get-only 巢狀集合，
    /// 物件形式送到 .NET 用戶端會靜默收不回來（決策 L7 踩過的形狀）。
    /// </summary>
    public class CustomizePluginSettingsMessagePackTests
    {
        private const string SampleXml =
            """<PluginSettings><Items><ProgramPluginItem ProgId="Order"><Plugins><PluginItem Type="A.B, A" /></Plugins></ProgramPluginItem></Items></PluginSettings>""";

        [Fact]
        [DisplayName("GetCustomizePluginSettingsRequest round-trip 保留 CustomizeId")]
        public void GetRequest_RoundTrip_PreservesCustomizeId()
        {
            var request = new GetCustomizePluginSettingsRequest { CustomizeId = "acme" };

            var restored = MessagePackCodec.Deserialize<GetCustomizePluginSettingsRequest>(
                MessagePackCodec.Serialize(request));

            Assert.NotNull(restored);
            Assert.Equal("acme", restored!.CustomizeId);
        }

        [Fact]
        [DisplayName("GetCustomizePluginSettingsResponse round-trip 保留 XML 原文")]
        public void GetResponse_RoundTrip_PreservesXml()
        {
            var response = new GetCustomizePluginSettingsResponse { Xml = SampleXml };

            var restored = MessagePackCodec.Deserialize<GetCustomizePluginSettingsResponse>(
                MessagePackCodec.Serialize(response));

            Assert.NotNull(restored);
            Assert.Equal(SampleXml, restored!.Xml);
        }

        [Fact]
        [DisplayName("SaveCustomizePluginSettingsRequest round-trip 保留兩個欄位")]
        public void SaveRequest_RoundTrip_PreservesBothFields()
        {
            var request = new SaveCustomizePluginSettingsRequest { CustomizeId = "acme", Xml = SampleXml };

            var restored = MessagePackCodec.Deserialize<SaveCustomizePluginSettingsRequest>(
                MessagePackCodec.Serialize(request));

            Assert.NotNull(restored);
            Assert.Equal("acme", restored!.CustomizeId);
            Assert.Equal(SampleXml, restored.Xml);
        }

        [Fact]
        [DisplayName("SaveCustomizePluginSettingsResponse round-trip 保留筆數")]
        public void SaveResponse_RoundTrip_PreservesCount()
        {
            var response = new SaveCustomizePluginSettingsResponse { PluginCount = 3 };

            var restored = MessagePackCodec.Deserialize<SaveCustomizePluginSettingsResponse>(
                MessagePackCodec.Serialize(response));

            Assert.NotNull(restored);
            Assert.Equal(3, restored!.PluginCount);
        }

        [Fact]
        [DisplayName("預設值 round-trip 不 NRE，空字串進空字串出")]
        public void DefaultValues_RoundTrip()
        {
            var restored = MessagePackCodec.Deserialize<SaveCustomizePluginSettingsRequest>(
                MessagePackCodec.Serialize(new SaveCustomizePluginSettingsRequest()));

            Assert.NotNull(restored);
            Assert.Equal(string.Empty, restored!.CustomizeId);
            Assert.Equal(string.Empty, restored.Xml);
        }
    }
}
