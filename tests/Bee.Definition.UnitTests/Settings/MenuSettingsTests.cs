using System.ComponentModel;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// MenuSettings、MenuFolder、MenuItem 等選單設定相關類別的測試。
    /// </summary>
    public class MenuSettingsTests
    {
        [Fact]
        [DisplayName("MenuItem 預設建構子應初始化為空字串")]
        public void MenuItem_DefaultConstructor_InitializesEmpty()
        {
            var item = new MenuItem();

            Assert.Equal(string.Empty, item.ProgId);
            Assert.Equal(string.Empty, item.DisplayName);
        }

        [Fact]
        [DisplayName("MenuItem 帶參數建構子應設定 ProgId 與 DisplayName")]
        public void MenuItem_ParameterizedConstructor_SetsProperties()
        {
            var item = new MenuItem("P001", "客戶維護");

            Assert.Equal("P001", item.ProgId);
            Assert.Equal("客戶維護", item.DisplayName);
            Assert.Equal("P001", item.Key);
        }

        [Fact]
        [DisplayName("MenuItem.Clone 應產生等值副本")]
        public void MenuItem_Clone_ProducesEqualCopy()
        {
            var item = new MenuItem("P001", "客戶維護");

            var clone = item.Clone();

            Assert.NotSame(item, clone);
            Assert.Equal(item.ProgId, clone.ProgId);
            Assert.Equal(item.DisplayName, clone.DisplayName);
        }

        [Fact]
        [DisplayName("MenuItem.ToString 應回傳 \"ProgId - DisplayName\"")]
        public void MenuItem_ToString_ReturnsFormatted()
        {
            var item = new MenuItem("P001", "客戶維護");

            Assert.Equal("P001 - 客戶維護", item.ToString());
        }

        [Fact]
        [DisplayName("MenuItemCollection Add(progId, displayName) 應新增並回傳項目")]
        public void MenuItemCollection_Add_AddsAndReturnsItem()
        {
            var folder = new MenuFolder("F01", "主檔");
            var collection = folder.Items!;

            var item = collection.Add("P001", "客戶維護");

            Assert.Single(collection);
            Assert.Equal("P001", item.ProgId);
            Assert.Equal("客戶維護", item.DisplayName);
        }

        [Fact]
        [DisplayName("MenuFolder 預設建構子應初始化為空字串")]
        public void MenuFolder_DefaultConstructor_InitializesEmpty()
        {
            var folder = new MenuFolder();

            Assert.Equal(string.Empty, folder.FolderId);
            Assert.Equal(string.Empty, folder.DisplayName);
        }

        [Fact]
        [DisplayName("MenuFolder 帶參數建構子應設定 FolderId 與 DisplayName")]
        public void MenuFolder_ParameterizedConstructor_SetsProperties()
        {
            var folder = new MenuFolder("F01", "主檔");

            Assert.Equal("F01", folder.FolderId);
            Assert.Equal("主檔", folder.DisplayName);
        }

        [Fact]
        [DisplayName("MenuFolder.Items 在未序列化狀態下應回傳集合")]
        public void MenuFolder_Items_ReturnsCollection()
        {
            var folder = new MenuFolder("F01", "主檔");

            var items = folder.Items;

            Assert.NotNull(items);
            items!.Add("P001", "客戶維護");
            Assert.Single(folder.Items!);
        }

        [Fact]
        [DisplayName("MenuFolder.FindItem 可找到同層項目")]
        public void MenuFolder_FindItem_FindsDirectItem()
        {
            var folder = new MenuFolder("F01", "主檔");
            folder.Items!.Add("P001", "客戶維護");
            folder.Items!.Add("P002", "商品維護");

            var found = folder.FindItem("P002");

            Assert.NotNull(found);
            Assert.Equal("P002", found!.ProgId);
        }

        [Fact]
        [DisplayName("MenuFolder.FindItem 找不到時應回傳 null")]
        public void MenuFolder_FindItem_NotFound_ReturnsNull()
        {
            var folder = new MenuFolder("F01", "主檔");
            folder.Items!.Add("P001", "客戶維護");

            var found = folder.FindItem("X999");

            Assert.Null(found);
        }

        [Fact]
        [DisplayName("MenuFolder.GetLanguageKey 應回傳 MenuFolder_<id>")]
        public void MenuFolder_GetLanguageKey_ReturnsFormatted()
        {
            var folder = new MenuFolder("F01", "主檔");

            Assert.Equal("MenuFolder_F01", folder.GetLanguageKey());
        }

        [Fact]
        [DisplayName("MenuFolder.ToString 應回傳 \"FolderId - DisplayName\"")]
        public void MenuFolder_ToString_ReturnsFormatted()
        {
            var folder = new MenuFolder("F01", "主檔");

            Assert.Equal("F01 - 主檔", folder.ToString());
        }

        [Fact]
        [DisplayName("MenuFolderCollection Add(id, displayName) 應新增並回傳項目")]
        public void MenuFolderCollection_Add_AddsAndReturnsItem()
        {
            var settings = new MenuSettings();
            var collection = settings.Folders!;

            var folder = collection.Add("F01", "主檔");

            Assert.Single(collection);
            Assert.Equal("F01", folder.FolderId);
            Assert.Equal("主檔", folder.DisplayName);
        }

        [Fact]
        [DisplayName("MenuSettings 預設狀態應有非空 Folders 與設定時間")]
        public void MenuSettings_Default_InitializesState()
        {
            var settings = new MenuSettings();

            Assert.NotNull(settings.Folders);
            Assert.NotEqual(default(DateTime), settings.CreateTime);
            Assert.Equal(string.Empty, settings.DisplayName);
        }

        [Fact]
        [DisplayName("MenuSettings.GetFolders 應扁平列出所有層級資料夾")]
        public void MenuSettings_GetFolders_ReturnsFlatList()
        {
            var settings = new MenuSettings();
            var root = settings.Folders!.Add("F01", "主檔");
            var sub = root.Folders!.Add("F01-1", "客戶");

            var folders = settings.GetFolders();

            Assert.Equal(2, folders.Count);
            Assert.Contains(folders, f => f.FolderId == "F01");
            Assert.Contains(folders, f => f.FolderId == "F01-1");
        }

        [Fact]
        [DisplayName("MenuSettings.GetItems 應扁平列出所有層級項目")]
        public void MenuSettings_GetItems_ReturnsFlatList()
        {
            var settings = new MenuSettings();
            var root = settings.Folders!.Add("F01", "主檔");
            root.Items!.Add("P001", "客戶維護");
            var sub = root.Folders!.Add("F01-1", "商品");
            sub.Items!.Add("P002", "商品維護");

            var items = settings.GetItems();

            Assert.Equal(2, items.Count);
            Assert.Contains(items, i => i.ProgId == "P001");
            Assert.Contains(items, i => i.ProgId == "P002");
        }

        [Fact]
        [DisplayName("MenuSettings.FindItem 可跨資料夾找到項目")]
        public void MenuSettings_FindItem_FindsAcrossFolders()
        {
            var settings = new MenuSettings();
            var root = settings.Folders!.Add("F01", "主檔");
            var sub = root.Folders!.Add("F01-1", "商品");
            sub.Items!.Add("P002", "商品維護");

            var found = settings.FindItem("P002");

            Assert.NotNull(found);
            Assert.Equal("P002", found!.ProgId);
        }

        [Fact]
        [DisplayName("MenuSettings.SetObjectFilePath 應更新檔案路徑")]
        public void MenuSettings_SetObjectFilePath_UpdatesPath()
        {
            var settings = new MenuSettings();

            settings.SetObjectFilePath("/tmp/menu.xml");

            Assert.Equal("/tmp/menu.xml", settings.ObjectFilePath);
        }
    }
}
