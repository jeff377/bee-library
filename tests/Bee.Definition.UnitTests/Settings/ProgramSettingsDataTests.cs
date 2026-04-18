using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// ProgramSettings、ProgramCategory、ProgramItem 等設定類別的測試。
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
            var category = new ProgramCategory("C01", "主檔");
            var collection = category.Items!;

            var item = collection.Add("P001", "客戶維護");

            Assert.Single(collection);
            Assert.Equal("P001", item.ProgId);
            Assert.Equal("客戶維護", item.DisplayName);
        }

        [Fact]
        [DisplayName("ProgramCategory 預設建構子應初始化為空字串")]
        public void ProgramCategory_DefaultConstructor_InitializesEmpty()
        {
            var category = new ProgramCategory();

            Assert.Equal(string.Empty, category.Id);
            Assert.Equal(string.Empty, category.DisplayName);
        }

        [Fact]
        [DisplayName("ProgramCategory 帶參數建構子應設定 Id 與 DisplayName")]
        public void ProgramCategory_ParameterizedConstructor_SetsProperties()
        {
            var category = new ProgramCategory("C01", "主檔");

            Assert.Equal("C01", category.Id);
            Assert.Equal("主檔", category.DisplayName);
        }

        [Fact]
        [DisplayName("ProgramCategory.Items 在未序列化狀態下應回傳集合")]
        public void ProgramCategory_Items_ReturnsCollection()
        {
            var category = new ProgramCategory("C01", "主檔");

            var items = category.Items;

            Assert.NotNull(items);
            items!.Add("P001", "客戶維護");
            Assert.Single(category.Items!);
        }

        [Fact]
        [DisplayName("ProgramCategory.ToString 應回傳 \"Id - DisplayName\"")]
        public void ProgramCategory_ToString_ReturnsFormatted()
        {
            var category = new ProgramCategory("C01", "主檔");

            Assert.Equal("C01 - 主檔", category.ToString());
        }

        [Fact]
        [DisplayName("ProgramCategory.SetSerializeState 應同步傳遞至 Items")]
        public void ProgramCategory_SetSerializeState_PropagatesToItems()
        {
            var category = new ProgramCategory("C01", "主檔");
            category.Items!.Add("P001", "客戶維護");

            category.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, category.SerializeState);
        }

        [Fact]
        [DisplayName("ProgramCategory.Items 於序列化且集合為空時應回傳 null")]
        public void ProgramCategory_Items_EmptyDuringSerialize_ReturnsNull()
        {
            var category = new ProgramCategory("C01", "主檔");
            category.SetSerializeState(SerializeState.Serialize);

            Assert.Null(category.Items);
        }

        [Fact]
        [DisplayName("ProgramCategoryCollection Add(id, displayName) 應新增並回傳項目")]
        public void ProgramCategoryCollection_Add_AddsAndReturnsItem()
        {
            var settings = new ProgramSettings();
            var collection = new ProgramCategoryCollection(settings);

            var category = collection.Add("C01", "主檔");

            Assert.Single(collection);
            Assert.Equal("C01", category.Id);
            Assert.Equal("主檔", category.DisplayName);
        }

        [Fact]
        [DisplayName("ProgramSettings 預設應有非空 Categories")]
        public void ProgramSettings_Default_HasCategories()
        {
            var settings = new ProgramSettings();

            Assert.NotNull(settings.Categories);
            Assert.Equal(SerializeState.None, settings.SerializeState);
            Assert.Equal(string.Empty, settings.ObjectFilePath);
        }

        [Fact]
        [DisplayName("ProgramSettings.SetSerializeState 應更新狀態")]
        public void ProgramSettings_SetSerializeState_UpdatesState()
        {
            var settings = new ProgramSettings();

            settings.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, settings.SerializeState);
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
