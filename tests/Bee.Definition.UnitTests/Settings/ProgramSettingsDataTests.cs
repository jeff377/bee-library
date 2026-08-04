using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{

    /// <summary>
    /// ProgramSettings、ProgramItem 等型別註冊表類別的測試。
    /// </summary>
    public class ProgramSettingsDataTests
    {
        [Fact]
        [DisplayName("ProgramItem 預設建構子應初始化為空字串")]
        public void ProgramItem_DefaultConstructor_InitializesEmpty()
        {
            var item = new ProgramItem();

            Assert.Equal(string.Empty, item.ProgId);
            Assert.Equal(string.Empty, item.DisplayName);
            Assert.Equal(string.Empty, item.BusinessObject);
        }

        [Fact]
        [DisplayName("ProgramItem.BusinessObject 預設應為空字串")]
        public void ProgramItem_BusinessObject_DefaultsToEmpty()
        {
            var item = new ProgramItem("P001", "客戶維護");

            Assert.Equal(string.Empty, item.BusinessObject);
        }

        [Fact]
        [DisplayName("ProgramItem.BusinessObject 為空時 XML 不應輸出該屬性")]
        public void ProgramItem_BusinessObject_EmptyOmittedFromXml()
        {
            var settings = new ProgramSettings();
            settings.Items!.Add("P001", "客戶維護");

            var xml = XmlCodec.Serialize(settings);

            Assert.DoesNotContain("BusinessObject=", xml);
        }

        [Fact]
        [DisplayName("ProgramItem.BusinessObject 有值時應透過 XmlAttribute 序列化往返")]
        public void ProgramItem_BusinessObject_RoundTripsThroughXml()
        {
            var settings = new ProgramSettings();
            var item = settings.Items!.Add("P001", "客戶維護");
            item.BusinessObject = "MyErp.Business.CustomerBo, MyErp.Business";

            var xml = XmlCodec.Serialize(settings);
            var restored = XmlCodec.Deserialize<ProgramSettings>(xml);

            Assert.Contains("BusinessObject=\"MyErp.Business.CustomerBo, MyErp.Business\"", xml);
            Assert.NotNull(restored);
            var restoredItem = restored!.Items!["P001"];
            Assert.Equal("MyErp.Business.CustomerBo, MyErp.Business", restoredItem.BusinessObject);
        }

        [Fact]
        [DisplayName("ProgramItem 帶參數建構子應設定 ProgId 與 DisplayName")]
        public void ProgramItem_ParameterizedConstructor_SetsProperties()
        {
            var item = new ProgramItem("P001", "客戶維護");

            Assert.Equal("P001", item.ProgId);
            Assert.Equal("客戶維護", item.DisplayName);
            Assert.Equal("P001", item.Key);
        }

        [Fact]
        [DisplayName("ProgramItem.ToString 應回傳 \"ProgId - DisplayName\"")]
        public void ProgramItem_ToString_ReturnsFormatted()
        {
            var item = new ProgramItem("P001", "客戶維護");

            Assert.Equal("P001 - 客戶維護", item.ToString());
        }

        [Fact]
        [DisplayName("ProgramItemCollection Add(progId, displayName) 應新增並回傳項目")]
        public void ProgramItemCollection_Add_AddsAndReturnsItem()
        {
            var settings = new ProgramSettings();
            var collection = settings.Items!;

            var item = collection.Add("P001", "客戶維護");

            Assert.Single(collection);
            Assert.Equal("P001", item.ProgId);
            Assert.Equal("客戶維護", item.DisplayName);
        }

        [Fact]
        [DisplayName("攤平後同一 progId 重複註冊應在載入期即被集合擋下")]
        public void ProgramItemCollection_DuplicateProgId_Throws()
        {
            var settings = new ProgramSettings();
            settings.Items!.Add("P001", "客戶維護");

            Assert.Throws<ArgumentException>(() => settings.Items!.Add("P001", "另一支程式"));
        }

        [Fact]
        [DisplayName("ProgramSettings 預設應有非空 Items")]
        public void ProgramSettings_Default_HasItems()
        {
            var settings = new ProgramSettings();

            Assert.NotNull(settings.Items);
            Assert.Equal(SerializeState.None, settings.SerializeState);
            Assert.Equal(string.Empty, settings.ObjectFilePath);
        }

        [Fact]
        [DisplayName("ProgramSettings.Items 於序列化且集合為空時應回傳 null")]
        public void ProgramSettings_Items_EmptyDuringSerialize_ReturnsNull()
        {
            var settings = new ProgramSettings();
            settings.SetSerializeState(SerializeState.Serialize);

            Assert.Null(settings.Items);
        }

        [Fact]
        [DisplayName("ProgramSettings.SetSerializeState 應更新狀態並傳遞至 Items")]
        public void ProgramSettings_SetSerializeState_UpdatesState()
        {
            var settings = new ProgramSettings();
            settings.Items!.Add("P001", "客戶維護");

            settings.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, settings.SerializeState);
            Assert.Equal(SerializeState.Serialize, settings.Items!.SerializeState);
        }

        [Fact]
        [DisplayName("ProgramSettings.SetObjectFilePath 應更新檔案路徑")]
        public void ProgramSettings_SetObjectFilePath_UpdatesPath()
        {
            var settings = new ProgramSettings();

            settings.SetObjectFilePath("/tmp/programs.xml");

            Assert.Equal("/tmp/programs.xml", settings.ObjectFilePath);
        }
    }
}
