using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// <see cref="PluginSettings"/> 的定義層行為：宣告順序即執行順序、同一 program 內型別不可重複、
    /// 時點隨型別一起 round-trip，以及缺 <c>Stage</c> 屬性時落到 <see cref="PluginStage.None"/>。
    /// </summary>
    public class PluginSettingsTests
    {
        private static readonly PluginBinding[] s_threeInOrder =
        [
            new("A.First, A", PluginStage.BeforeSave),
            new("A.Second, A", PluginStage.AfterSave),
            new("A.Third, A", PluginStage.AfterDelete),
        ];

        private static readonly PluginBinding[] s_orderChain =
        [
            new("A.CreditLimit, A", PluginStage.BeforeSave),
            new("A.Sync, A", PluginStage.AfterSave),
        ];

        private static readonly PluginBinding[] s_customerChain =
        [
            new("A.Dedupe, A", PluginStage.BeforeSave),
        ];

        [Fact]
        [DisplayName("GetPluginBindings 依宣告順序回傳，順序即執行順序")]
        public void GetPluginBindings_ReturnsDeclarationOrder()
        {
            var settings = new PluginSettings();
            var program = settings.Items!.Add("Order");
            program.Plugins!.Add("A.First, A", PluginStage.BeforeSave);
            program.Plugins!.Add("A.Second, A", PluginStage.AfterSave);
            program.Plugins!.Add("A.Third, A", PluginStage.AfterDelete);

            Assert.Equal(s_threeInOrder, settings.GetPluginBindings("Order"));
        }

        [Fact]
        [DisplayName("GetPluginBindings 對未宣告的 progId 回空集合，不回 null")]
        public void GetPluginBindings_UnknownProgId_ReturnsEmpty()
        {
            var settings = new PluginSettings();
            settings.Items!.Add("Order").Plugins!.Add("A.First, A", PluginStage.BeforeSave);

            Assert.Empty(settings.GetPluginBindings("Customer"));
            Assert.Empty(new PluginSettings().GetPluginBindings("Order"));
        }

        [Fact]
        [DisplayName("GetPluginBindings 回傳的是值複本，改它不會動到 cache 裡的定義")]
        public void GetPluginBindings_ReturnsValueCopies()
        {
            var settings = new PluginSettings();
            settings.Items!.Add("Order").Plugins!.Add("A.First, A", PluginStage.BeforeSave);

            var binding = Assert.Single(settings.GetPluginBindings("Order"));
            _ = binding with { Stage = PluginStage.AfterDelete };

            Assert.Equal(PluginStage.BeforeSave,
                Assert.Single(settings.GetPluginBindings("Order")).Stage);
        }

        [Fact]
        [DisplayName("同一 program 內重複宣告同一型別應在加入時就被拒")]
        public void Plugins_DuplicateType_Throws()
        {
            var program = new PluginSettings().Items!.Add("Order");
            program.Plugins!.Add("A.First, A", PluginStage.BeforeSave);

            Assert.Throws<ArgumentException>(
                () => program.Plugins!.Add("A.First, A", PluginStage.AfterSave));
        }

        [Fact]
        [DisplayName("不同 program 可各自宣告同一型別")]
        public void Plugins_SameTypeUnderDifferentPrograms_Allowed()
        {
            var settings = new PluginSettings();
            settings.Items!.Add("Order").Plugins!.Add("A.Shared, A", PluginStage.BeforeSave);
            settings.Items!.Add("Customer").Plugins!.Add("A.Shared, A", PluginStage.BeforeSave);

            Assert.Equal("A.Shared, A", Assert.Single(settings.GetPluginBindings("Order")).Type);
            Assert.Equal("A.Shared, A", Assert.Single(settings.GetPluginBindings("Customer")).Type);
        }

        [Fact]
        [DisplayName("XML round-trip 後 progId、鏈序與各自的時點皆保留")]
        public void XmlRoundTrip_PreservesProgramsOrderAndStages()
        {
            var settings = new PluginSettings();
            var order = settings.Items!.Add("Order");
            order.Plugins!.Add("A.CreditLimit, A", PluginStage.BeforeSave);
            order.Plugins!.Add("A.Sync, A", PluginStage.AfterSave);
            settings.Items!.Add("Customer").Plugins!.Add("A.Dedupe, A", PluginStage.BeforeSave);

            var restored = XmlCodec.Deserialize<PluginSettings>(XmlCodec.Serialize(settings))!;

            Assert.Equal(s_orderChain, restored.GetPluginBindings("Order"));
            Assert.Equal(s_customerChain, restored.GetPluginBindings("Customer"));
        }

        [Fact]
        [DisplayName("時點寫成 XML 屬性，手寫檔一眼看得出哪個 plugin 跑在哪個時點")]
        public void Serialize_WritesStageAsXmlAttribute()
        {
            var settings = new PluginSettings();
            settings.Items!.Add("Order").Plugins!.Add("A.CreditLimit, A", PluginStage.BeforeSave);

            string xml = XmlCodec.Serialize(settings);

            Assert.Contains(@"Type=""A.CreditLimit, A""", xml, StringComparison.Ordinal);
            Assert.Contains(@"Stage=""BeforeSave""", xml, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("★手寫檔漏了 Stage 屬性時落到 None，而不是靜靜變成第一個時點")]
        public void Deserialize_MissingStageAttribute_YieldsNone()
        {
            // 少一個 XmlAttribute 不會有任何錯誤，屬性會拿到型別預設值——列舉的 0 值因此保留給
            // 「沒宣告」，讓兩道閘門講得出「你沒宣告 Stage」而不是一句對不上的時點比較。
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <PluginSettings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Items>
                    <ProgramPluginItem ProgId="Order">
                      <Plugins>
                        <PluginItem Type="A.CreditLimit, A" />
                      </Plugins>
                    </ProgramPluginItem>
                  </Items>
                </PluginSettings>
                """;

            var restored = XmlCodec.Deserialize<PluginSettings>(xml)!;

            Assert.Equal(PluginStage.None, Assert.Single(restored.GetPluginBindings("Order")).Stage);
        }

        [Fact]
        [DisplayName("空的 PluginSettings round-trip 後仍可用，且不含任何 program")]
        public void XmlRoundTrip_Empty_StaysUsable()
        {
            var restored = XmlCodec.Deserialize<PluginSettings>(XmlCodec.Serialize(new PluginSettings()))!;

            Assert.Empty(restored.GetPluginBindings("Order"));
        }
    }
}
