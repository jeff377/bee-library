using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// <see cref="PluginSettings"/> 的定義層行為：宣告順序即執行順序、同一 program 內型別不可重複、
    /// XML round-trip 保序。
    /// </summary>
    public class PluginSettingsTests
    {
        private static readonly string[] s_threeInOrder = ["A.First, A", "A.Second, A", "A.Third, A"];
        private static readonly string[] s_orderChain = ["A.CreditLimit, A", "A.Sync, A"];
        private static readonly string[] s_customerChain = ["A.Dedupe, A"];

        [Fact]
        [DisplayName("GetPluginTypes 依宣告順序回傳，順序即執行順序")]
        public void GetPluginTypes_ReturnsDeclarationOrder()
        {
            var settings = new PluginSettings();
            var program = settings.Items!.Add("Order");
            program.Plugins!.Add("A.First, A");
            program.Plugins!.Add("A.Second, A");
            program.Plugins!.Add("A.Third, A");

            Assert.Equal(s_threeInOrder, settings.GetPluginTypes("Order"));
        }

        [Fact]
        [DisplayName("GetPluginTypes 對未宣告的 progId 回空集合，不回 null")]
        public void GetPluginTypes_UnknownProgId_ReturnsEmpty()
        {
            var settings = new PluginSettings();
            settings.Items!.Add("Order").Plugins!.Add("A.First, A");

            Assert.Empty(settings.GetPluginTypes("Customer"));
            Assert.Empty(new PluginSettings().GetPluginTypes("Order"));
        }

        [Fact]
        [DisplayName("同一 program 內重複宣告同一型別應在加入時就被拒")]
        public void Plugins_DuplicateType_Throws()
        {
            var program = new PluginSettings().Items!.Add("Order");
            program.Plugins!.Add("A.First, A");

            Assert.Throws<ArgumentException>(() => program.Plugins!.Add("A.First, A"));
        }

        [Fact]
        [DisplayName("不同 program 可各自宣告同一型別")]
        public void Plugins_SameTypeUnderDifferentPrograms_Allowed()
        {
            var settings = new PluginSettings();
            settings.Items!.Add("Order").Plugins!.Add("A.Shared, A");
            settings.Items!.Add("Customer").Plugins!.Add("A.Shared, A");

            Assert.Equal("A.Shared, A", Assert.Single(settings.GetPluginTypes("Order")));
            Assert.Equal("A.Shared, A", Assert.Single(settings.GetPluginTypes("Customer")));
        }

        [Fact]
        [DisplayName("XML round-trip 後 progId 與各自的鏈序皆保留")]
        public void XmlRoundTrip_PreservesProgramsAndOrder()
        {
            var settings = new PluginSettings();
            var order = settings.Items!.Add("Order");
            order.Plugins!.Add("A.CreditLimit, A");
            order.Plugins!.Add("A.Sync, A");
            settings.Items!.Add("Customer").Plugins!.Add("A.Dedupe, A");

            var restored = XmlCodec.Deserialize<PluginSettings>(XmlCodec.Serialize(settings))!;

            Assert.Equal(s_orderChain, restored.GetPluginTypes("Order"));
            Assert.Equal(s_customerChain, restored.GetPluginTypes("Customer"));
        }

        [Fact]
        [DisplayName("空的 PluginSettings round-trip 後仍可用，且不含任何 program")]
        public void XmlRoundTrip_Empty_StaysUsable()
        {
            var restored = XmlCodec.Deserialize<PluginSettings>(XmlCodec.Serialize(new PluginSettings()))!;

            Assert.Empty(restored.GetPluginTypes("Order"));
        }
    }
}
