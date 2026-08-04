using System.ComponentModel;
using Bee.Definition.Customization;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Customization
{
    /// <summary>
    /// <see cref="CustomizeOverlay"/> 測試：四種型別各自的選用粒度，以及任一層缺席時的行為。
    /// 這個類別是前後端共用的選用演算法，兩端行為一致與否全繫於此，所以粒度要逐條釘住。
    /// </summary>
    public class CustomizeOverlayTests
    {
        // ---- 語系文字：per key ----

        [Fact]
        [DisplayName("文字：客製有該 key 時取客製值")]
        public void TryGetLangText_CustomizeHasKey_ReturnsCustomizeValue()
        {
            var cust = Resource(("OK", "送出"));
            var @base = Resource(("OK", "確定"), ("Cancel", "取消"));

            Assert.True(CustomizeOverlay.TryGetLangText(cust, @base, "OK", out var text));
            Assert.Equal("送出", text);
        }

        [Fact]
        [DisplayName("文字：客製沒有該 key 時延用套裝值")]
        public void TryGetLangText_CustomizeMissesKey_FallsBackToBase()
        {
            var cust = Resource(("OK", "送出"));
            var @base = Resource(("OK", "確定"), ("Cancel", "取消"));

            Assert.True(CustomizeOverlay.TryGetLangText(cust, @base, "Cancel", out var text));
            Assert.Equal("取消", text);
        }

        [Fact]
        [DisplayName("文字：客製獨有的 key 也查得到（套裝無則加入）")]
        public void TryGetLangText_CustomizeOnlyKey_IsFound()
        {
            var cust = Resource(("OnlyInCustomize", "客製獨有"));
            var @base = Resource(("OK", "確定"));

            Assert.True(CustomizeOverlay.TryGetLangText(cust, @base, "OnlyInCustomize", out var text));
            Assert.Equal("客製獨有", text);
        }

        [Fact]
        [DisplayName("文字：客製為 null 時等同純套裝")]
        public void TryGetLangText_NoCustomize_UsesBase()
        {
            Assert.True(CustomizeOverlay.TryGetLangText(null, Resource(("OK", "確定")), "OK", out var text));
            Assert.Equal("確定", text);
        }

        [Fact]
        [DisplayName("文字：套裝為 null 時仍可從客製取得")]
        public void TryGetLangText_NoBase_UsesCustomize()
        {
            Assert.True(CustomizeOverlay.TryGetLangText(Resource(("OK", "送出")), null, "OK", out var text));
            Assert.Equal("送出", text);
        }

        [Fact]
        [DisplayName("文字：兩層皆無該 key 時回 false 且 text 為空字串")]
        public void TryGetLangText_BothMiss_ReturnsFalse()
        {
            Assert.False(CustomizeOverlay.TryGetLangText(Resource(), Resource(), "Nope", out var text));
            Assert.Equal(string.Empty, text);
        }

        [Fact]
        [DisplayName("文字：兩層皆 null 時回 false，不丟例外")]
        public void TryGetLangText_BothNull_ReturnsFalse()
        {
            Assert.False(CustomizeOverlay.TryGetLangText(null, null, "Nope", out var text));
            Assert.Equal(string.Empty, text);
        }

        // ---- 語系 enum：整組取代 ----

        [Fact]
        [DisplayName("enum：客製有同名 enum 時整組取代，套裝獨有的 entry 不保留")]
        public void GetLangEnum_CustomizeHasEnum_ReplacesWholeSet()
        {
            var cust = ResourceWithEnum("Gender", ("M", "先生"));
            var @base = ResourceWithEnum("Gender", ("M", "男"), ("F", "女"));

            var result = CustomizeOverlay.GetLangEnum(cust, @base, "Gender");

            Assert.NotNull(result);
            Assert.Single(result!.Entries);
            Assert.Equal("先生", result.GetText("M"));
            Assert.Null(result.GetText("F"));
        }

        [Fact]
        [DisplayName("enum：客製沒有該 enum 時延用套裝整組")]
        public void GetLangEnum_CustomizeMissesEnum_FallsBackToBase()
        {
            var cust = Resource(("OK", "送出"));
            var @base = ResourceWithEnum("Gender", ("M", "男"));

            var result = CustomizeOverlay.GetLangEnum(cust, @base, "Gender");

            Assert.NotNull(result);
            Assert.Equal("男", result!.GetText("M"));
        }

        [Fact]
        [DisplayName("enum：兩層皆無時回 null")]
        public void GetLangEnum_BothMiss_ReturnsNull()
        {
            Assert.Null(CustomizeOverlay.GetLangEnum(null, null, "Gender"));
        }

        // ---- ProgramSettings：per progId ----

        [Fact]
        [DisplayName("ProgramItem：客製有該 progId 時取客製項目")]
        public void FindProgramItem_CustomizeHasProgId_ReturnsCustomizeItem()
        {
            var cust = Settings(("P001", "Cust.Bo"));
            var @base = Settings(("P001", "Base.Bo"), ("P002", "Base.Bo2"));

            Assert.Equal("Cust.Bo", CustomizeOverlay.FindProgramItem(cust, @base, "P001")!.BusinessObject);
        }

        [Fact]
        [DisplayName("ProgramItem：客製沒有該 progId 時延用套裝項目（per progId 而非整檔）")]
        public void FindProgramItem_CustomizeMissesProgId_FallsBackToBase()
        {
            var cust = Settings(("P001", "Cust.Bo"));
            var @base = Settings(("P001", "Base.Bo"), ("P002", "Base.Bo2"));

            Assert.Equal("Base.Bo2", CustomizeOverlay.FindProgramItem(cust, @base, "P002")!.BusinessObject);
        }

        [Fact]
        [DisplayName("ProgramItem：兩層皆無該 progId 或皆為 null 時回 null")]
        public void FindProgramItem_BothMiss_ReturnsNull()
        {
            Assert.Null(CustomizeOverlay.FindProgramItem(Settings(), Settings(), "P999"));
            Assert.Null(CustomizeOverlay.FindProgramItem(null, null, "P001"));
        }

        // ---- FormLayout：整檔取代 ----

        [Fact]
        [DisplayName("FormLayout：客製存在時整檔取代")]
        public void PickFormLayout_CustomizeExists_WinsOutright()
        {
            var cust = new FormLayout { LayoutId = "Employee", Caption = "客製" };
            var @base = new FormLayout { LayoutId = "Employee", Caption = "套裝" };

            Assert.Same(cust, CustomizeOverlay.PickFormLayout(cust, @base));
        }

        [Fact]
        [DisplayName("FormLayout：客製不存在時回套裝；兩者皆無時回 null（由呼叫端改用生成）")]
        public void PickFormLayout_FallsBackThenNull()
        {
            var @base = new FormLayout { LayoutId = "Employee" };

            Assert.Same(@base, CustomizeOverlay.PickFormLayout(null, @base));
            Assert.Null(CustomizeOverlay.PickFormLayout(null, null));
        }

        [Fact]
        [DisplayName("MenuSettings：客製存在時整份取代套裝（不逐節點合併）")]
        public void PickMenuSettings_CustomizeExists_WinsOutright()
        {
            var cust = new MenuSettings();
            cust.Items!.AddEntry("tenant", "Order", "客製選單");
            var @base = new MenuSettings();
            @base.Items!.AddEntry("standard", "Order", "套裝選單");

            Assert.Same(cust, CustomizeOverlay.PickMenuSettings(cust, @base));
        }

        [Fact]
        [DisplayName("MenuSettings：客製不存在時回套裝；兩者皆無時回 null")]
        public void PickMenuSettings_FallsBackThenNull()
        {
            var @base = new MenuSettings();

            Assert.Same(@base, CustomizeOverlay.PickMenuSettings(null, @base));
            Assert.Null(CustomizeOverlay.PickMenuSettings(null, null));
        }

        [Fact]
        [DisplayName("ProgramSettings：攤平後仍為 per-progId 覆寫，客製未宣告者落回套裝")]
        public void FindProgramItem_FlatRegistry_OverlaysPerProgId()
        {
            var cust = Settings(("Order", "Tenant.OrderBO, Tenant"));
            var @base = Settings(("Order", "Pkg.OrderBO, Pkg"), ("Customer", "Pkg.CustomerBO, Pkg"));

            Assert.Equal("Tenant.OrderBO, Tenant",
                CustomizeOverlay.FindProgramItem(cust, @base, "Order")!.BusinessObject);
            Assert.Equal("Pkg.CustomerBO, Pkg",
                CustomizeOverlay.FindProgramItem(cust, @base, "Customer")!.BusinessObject);
            Assert.Null(CustomizeOverlay.FindProgramItem(cust, @base, "Nope"));
        }

        // ---- Fixtures ----

        private static LanguageResource Resource(params (string Key, string Value)[] items)
        {
            var resource = new LanguageResource { Namespace = "Common", Lang = "zh-TW" };
            foreach (var (key, value) in items)
                resource.Items.Add(key, value);
            return resource;
        }

        private static LanguageResource ResourceWithEnum(string enumName, params (string Code, string Text)[] entries)
        {
            var resource = Resource();
            var langEnum = new LanguageEnum { Name = enumName };
            foreach (var (code, text) in entries)
                langEnum.Entries.Add(code, text);
            resource.Enums.Add(langEnum);
            return resource;
        }

        private static ProgramSettings Settings(params (string ProgId, string BusinessObject)[] items)
        {
            var settings = new ProgramSettings();
            foreach (var (progId, bo) in items)
                settings.Items!.Add(progId, progId).BusinessObject = bo;
            return settings;
        }
    }
}
