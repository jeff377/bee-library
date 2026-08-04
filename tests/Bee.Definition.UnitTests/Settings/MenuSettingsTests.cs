using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// MenuSettings、MenuFolder、MenuEntry 等選單定義類別的測試。
    /// </summary>
    public class MenuSettingsTests
    {
        /// <summary>
        /// 建立三層巢狀選單：root → transactions →（customer、sales →（sales-order、sales-return））、dashboard。
        /// </summary>
        private static MenuSettings BuildNestedMenu()
        {
            var settings = new MenuSettings();
            var transactions = settings.Items!.AddFolder("transactions", "交易");
            transactions.Order = 10;
            transactions.Items!.AddEntry("customer", "Customer", "客戶").Order = 10;

            var sales = transactions.Items!.AddFolder("sales", "銷售");
            sales.Order = 20;
            sales.Items!.AddEntry("sales-order", "Order", "訂單").Order = 10;
            sales.Items!.AddEntry("sales-return", "Order", "退貨單").Order = 20;

            settings.Items!.AddEntry("dashboard", "Dashboard", "儀表板").Order = 20;
            return settings;
        }

        [Fact]
        [DisplayName("MenuSettings 預設應有非空 Items 與初始序列化狀態")]
        public void MenuSettings_Default_HasItems()
        {
            var settings = new MenuSettings();

            Assert.NotNull(settings.Items);
            Assert.Equal(SerializeState.None, settings.SerializeState);
            Assert.Equal(string.Empty, settings.ObjectFilePath);
        }

        [Fact]
        [DisplayName("MenuSettings.SetObjectFilePath 應更新檔案路徑")]
        public void MenuSettings_SetObjectFilePath_UpdatesPath()
        {
            var settings = new MenuSettings();

            settings.SetObjectFilePath("/tmp/menu.xml");

            Assert.Equal("/tmp/menu.xml", settings.ObjectFilePath);
        }

        [Fact]
        [DisplayName("MenuSettings.SetSerializeState 應傳遞至 Items")]
        public void MenuSettings_SetSerializeState_PropagatesToItems()
        {
            var settings = BuildNestedMenu();

            settings.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, settings.SerializeState);
            Assert.Equal(SerializeState.Serialize, settings.Items!.SerializeState);
        }

        [Fact]
        [DisplayName("MenuSettings.Items 於序列化且集合為空時應回傳 null")]
        public void MenuSettings_Items_EmptyDuringSerialize_ReturnsNull()
        {
            var settings = new MenuSettings();
            settings.SetSerializeState(SerializeState.Serialize);

            Assert.Null(settings.Items);
        }

        [Fact]
        [DisplayName("三層巢狀選單經 XML 往返應完整還原結構與型別")]
        public void MenuSettings_DeepNested_XmlRoundtrip_PreservesStructure()
        {
            var xml = XmlCodec.Serialize(BuildNestedMenu());

            var restored = XmlCodec.Deserialize<MenuSettings>(xml);

            Assert.NotNull(restored);
            Assert.Equal(2, restored!.Items!.Count);
            var transactions = Assert.IsType<MenuFolder>(restored.Items![0]);
            Assert.Equal(2, transactions.Items!.Count);
            Assert.IsType<MenuEntry>(transactions.Items![0]);

            var sales = Assert.IsType<MenuFolder>(transactions.Items![1]);
            Assert.Equal(2, sales.Items!.Count);
            var salesReturn = Assert.IsType<MenuEntry>(sales.Items![1]);
            Assert.Equal("sales-return", salesReturn.Id);
            Assert.Equal("Order", salesReturn.ProgId);
            Assert.Equal(20, salesReturn.Order);

            Assert.IsType<MenuEntry>(restored.Items![1]);
        }

        [Fact]
        [DisplayName("多型節點應輸出各自的元素名而非 xsi:type 判別碼")]
        public void MenuSettings_Xml_UsesPerSubtypeElementNames()
        {
            var xml = XmlCodec.Serialize(BuildNestedMenu());

            Assert.Contains("<MenuFolder ", xml, StringComparison.Ordinal);
            Assert.Contains("<MenuEntry ", xml, StringComparison.Ordinal);
            Assert.DoesNotContain("xsi:type", xml, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("空的 MenuFolder 於序列化時不應輸出 Items 元素")]
        public void MenuFolder_EmptyItems_OmittedFromXml()
        {
            var settings = new MenuSettings();
            settings.Items!.AddFolder("empty", "空資料夾");

            var xml = XmlCodec.Serialize(settings);

            Assert.Contains("<MenuFolder ", xml, StringComparison.Ordinal);
            Assert.DoesNotContain("<Items />", xml, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("EnumerateNodes 應以深度優先、文件順序走訪整棵樹")]
        public void EnumerateNodes_WalksWholeTreeDepthFirst()
        {
            var ids = BuildNestedMenu().EnumerateNodes().Select(n => n.Id).ToArray();

            Assert.Equal(
                ["transactions", "customer", "sales", "sales-order", "sales-return", "dashboard"],
                ids);
        }

        [Fact]
        [DisplayName("FindNode 應可跨層級以 Id 找到節點")]
        public void FindNode_FindsNestedNode()
        {
            var settings = BuildNestedMenu();

            var node = settings.FindNode("sales-return");

            var entry = Assert.IsType<MenuEntry>(node);
            Assert.Equal("Order", entry.ProgId);
        }

        [Fact]
        [DisplayName("FindNode 找不到時應回傳 null")]
        public void FindNode_Missing_ReturnsNull()
        {
            Assert.Null(BuildNestedMenu().FindNode("nope"));
        }

        [Fact]
        [DisplayName("同一 progId 可對應多個選單節點（1:N）")]
        public void MenuEntry_SameProgId_MayAppearInSeveralNodes()
        {
            var settings = BuildNestedMenu();

            var forOrder = settings.EnumerateNodes().OfType<MenuEntry>()
                .Where(e => e.ProgId == "Order").Select(e => e.Id).ToArray();

            Assert.Equal(["sales-order", "sales-return"], forOrder);
        }

        [Fact]
        [DisplayName("GetDisplayNodes 應濾掉不可見節點並依 Order 排序")]
        public void GetDisplayNodes_FiltersInvisibleAndSortsByOrder()
        {
            var settings = new MenuSettings();
            settings.Items!.AddEntry("c", "C", "C").Order = 30;
            settings.Items!.AddEntry("a", "A", "A").Order = 10;
            var hidden = settings.Items!.AddEntry("b", "B", "B");
            hidden.Order = 20;
            hidden.Visible = false;

            var ids = settings.Items!.GetDisplayNodes().Select(n => n.Id).ToArray();

            Assert.Equal(["a", "c"], ids);
        }

        [Fact]
        [DisplayName("Order 相同時 GetDisplayNodes 應維持文件順序")]
        public void GetDisplayNodes_EqualOrder_KeepsDocumentOrder()
        {
            var settings = new MenuSettings();
            settings.Items!.AddEntry("first", "A", "A");
            settings.Items!.AddEntry("second", "B", "B");

            var ids = settings.Items!.GetDisplayNodes().Select(n => n.Id).ToArray();

            Assert.Equal(["first", "second"], ids);
        }

        [Fact]
        [DisplayName("結構完整的選單 Validate 應無任何問題")]
        public void Validate_ValidMenu_ReturnsEmpty()
        {
            Assert.Empty(BuildNestedMenu().Validate());
        }

        [Fact]
        [DisplayName("Id 跨層級重複應被 Validate 抓出（同層由集合本身擋下）")]
        public void Validate_DuplicateIdAcrossLevels_IsReported()
        {
            var settings = new MenuSettings();
            var folder = settings.Items!.AddFolder("shared", "資料夾");
            // Sibling uniqueness is the collection's job; this duplicate is a level deeper, which
            // is exactly the gap the tree walk exists to close.
            folder.Items!.AddEntry("shared", "Order", "訂單");

            var problems = settings.Validate();

            Assert.Contains(problems, p => p.Contains("'shared'", StringComparison.Ordinal));
        }

        [Fact]
        [DisplayName("節點 Id 為空應被 Validate 抓出")]
        public void Validate_EmptyId_IsReported()
        {
            var settings = new MenuSettings();
            settings.Items!.Add(new MenuEntry { ProgId = "Order", Caption = "訂單" });

            var problems = settings.Validate();

            Assert.Contains(problems, p => p.Contains("empty Id", StringComparison.Ordinal));
        }

        [Fact]
        [DisplayName("MenuEntry.ProgId 為空應被 Validate 抓出")]
        public void Validate_EmptyProgId_IsReported()
        {
            var settings = new MenuSettings();
            settings.Items!.AddEntry("orphan", string.Empty, "無綁定");

            var problems = settings.Validate();

            Assert.Contains(problems, p => p.Contains("empty ProgId", StringComparison.Ordinal));
        }

        [Fact]
        [DisplayName("選單引用註冊表中不存在的 progId 應被 Validate 抓出")]
        public void Validate_UnregisteredProgId_IsReported()
        {
            var registry = new ProgramSettings();
            registry.Items!.Add("Customer", "客戶");
            var settings = new MenuSettings();
            settings.Items!.AddEntry("ghost", "NoSuchProgram", "幽靈");

            var problems = settings.Validate(registry);

            Assert.Contains(problems, p => p.Contains("NoSuchProgram", StringComparison.Ordinal));
        }

        [Fact]
        [DisplayName("未提供註冊表時 Validate 應跳過參照完整性檢查")]
        public void Validate_NoRegistry_SkipsReferentialCheck()
        {
            var settings = new MenuSettings();
            settings.Items!.AddEntry("ghost", "NoSuchProgram", "幽靈");

            Assert.Empty(settings.Validate());
        }

        [Fact]
        [DisplayName("EnsureValid 於選單有問題時應拋出並列出全部問題")]
        public void EnsureValid_Invalid_ThrowsListingAllProblems()
        {
            var settings = new MenuSettings();
            var folder = settings.Items!.AddFolder("dup", "資料夾");
            folder.Items!.AddEntry("dup", string.Empty, "重複且無 ProgId");

            var ex = Assert.Throws<InvalidOperationException>(() => settings.EnsureValid());

            Assert.Contains("'dup'", ex.Message, StringComparison.Ordinal);
            Assert.Contains("empty ProgId", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("EnsureValid 於選單正確時不應拋出")]
        public void EnsureValid_Valid_DoesNotThrow()
        {
            var exception = Record.Exception(() => BuildNestedMenu().EnsureValid());

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("MenuFolder.ToString / MenuEntry.ToString 應回傳可辨識字串")]
        public void ToString_ReturnsIdentifiableText()
        {
            Assert.Equal("sales - 銷售", new MenuFolder("sales", "銷售").ToString());
            Assert.Equal("order - 訂單 (Order)", new MenuEntry("order", "Order", "訂單").ToString());
        }

        [Fact]
        [DisplayName("MenuNodeBase.Visible 預設應為 true")]
        public void MenuNode_Visible_DefaultsToTrue()
        {
            Assert.True(new MenuFolder().Visible);
            Assert.True(new MenuEntry().Visible);
        }
    }
}
