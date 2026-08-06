using System.ComponentModel;
using System.Reflection;
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
        private static readonly string[] s_pkgChain = ["Pkg.Audit, Pkg", "Pkg.Numbering, Pkg"];
        private static readonly string[] s_custChain = ["Cust.CreditLimit, Cust"];
        private static readonly string[] s_concatenated =
            ["Pkg.Audit, Pkg", "Pkg.Numbering, Pkg", "Cust.CreditLimit, Cust"];
        private static readonly string[] s_pkgAuditOnly = ["Pkg.Audit, Pkg"];
        private static readonly string[] s_custDedupeOnly = ["Cust.Dedupe, Cust"];

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

        [Fact]
        [DisplayName("ProgramItem：客製只寫 BusinessObject 時 Repository 沿用套裝")]
        public void FindProgramItem_CustomizeOmitsRepository_InheritsBaseRepository()
        {
            var cust = Settings(("Order", "Tenant.OrderBO, Tenant"));
            var @base = new ProgramSettings();
            @base.Items!.Add(new ProgramItem("Order", "訂單")
            {
                BusinessObject = "Pkg.OrderBO, Pkg",
                Repository = "Pkg.OrderRepository, Pkg",
            });

            var merged = CustomizeOverlay.FindProgramItem(cust, @base, "Order")!;

            Assert.Equal("Tenant.OrderBO, Tenant", merged.BusinessObject);
            Assert.Equal("Pkg.OrderRepository, Pkg", merged.Repository);
        }

        [Fact]
        [DisplayName("ProgramItem：客製只寫 Repository 時 BusinessObject 沿用套裝")]
        public void FindProgramItem_CustomizeOmitsBusinessObject_InheritsBaseBusinessObject()
        {
            var cust = new ProgramSettings();
            cust.Items!.Add(new ProgramItem("Order", string.Empty)
            {
                Repository = "Tenant.OrderRepository, Tenant",
            });
            var @base = new ProgramSettings();
            @base.Items!.Add(new ProgramItem("Order", "訂單")
            {
                BusinessObject = "Pkg.OrderBO, Pkg",
                Repository = "Pkg.OrderRepository, Pkg",
            });

            var merged = CustomizeOverlay.FindProgramItem(cust, @base, "Order")!;

            Assert.Equal("Pkg.OrderBO, Pkg", merged.BusinessObject);
            Assert.Equal("Tenant.OrderRepository, Tenant", merged.Repository);
            Assert.Equal("訂單", merged.DisplayName);
        }

        [Fact]
        [DisplayName("ProgramItem：合成產生新實例，兩層的快取實例都不被異動")]
        public void FindProgramItem_BothLayersDeclare_ReturnsNewInstanceWithoutMutatingEither()
        {
            var cust = Settings(("Order", "Tenant.OrderBO, Tenant"));
            var @base = new ProgramSettings();
            @base.Items!.Add(new ProgramItem("Order", "訂單")
            {
                BusinessObject = "Pkg.OrderBO, Pkg",
                Repository = "Pkg.OrderRepository, Pkg",
            });

            var merged = CustomizeOverlay.FindProgramItem(cust, @base, "Order")!;

            Assert.NotSame(cust.Items!["Order"], merged);
            Assert.NotSame(@base.Items!["Order"], merged);
            Assert.Equal(string.Empty, cust.Items!["Order"].Repository);
            Assert.Equal("Pkg.OrderBO, Pkg", @base.Items!["Order"].BusinessObject);
        }

        [Fact]
        [DisplayName("ProgramItem：只有一層宣告時直接回該層實例，不做多餘配置")]
        public void FindProgramItem_SingleLayerDeclares_ReturnsThatInstance()
        {
            var cust = Settings(("Order", "Tenant.OrderBO, Tenant"));
            var @base = Settings(("Customer", "Pkg.CustomerBO, Pkg"));

            Assert.Same(cust.Items!["Order"], CustomizeOverlay.FindProgramItem(cust, @base, "Order"));
            Assert.Same(@base.Items!["Customer"], CustomizeOverlay.FindProgramItem(cust, @base, "Customer"));
        }

        [Fact]
        [DisplayName("ProgramItem：每個可寫字串屬性都參與欄位級合成（新增屬性未同步就會紅）")]
        public void FindProgramItem_EveryWritableStringProperty_TakesPartInTheMerge()
        {
            var properties = MergedProgramItemProperties();
            Assert.NotEmpty(properties);

            foreach (var property in properties)
            {
                // 套裝每個屬性都有值；客製只填目前受測的這一個。
                var baseItem = new ProgramItem { ProgId = "Order" };
                foreach (var other in properties)
                    other.SetValue(baseItem, $"base-{other.Name}");

                var customizeItem = new ProgramItem { ProgId = "Order" };
                property.SetValue(customizeItem, $"cust-{property.Name}");

                var cust = new ProgramSettings();
                cust.Items!.Add(customizeItem);
                var @base = new ProgramSettings();
                @base.Items!.Add(baseItem);

                var merged = CustomizeOverlay.FindProgramItem(cust, @base, "Order")!;

                Assert.Equal($"cust-{property.Name}", property.GetValue(merged));
                foreach (var other in properties.Where(p => p.Name != property.Name))
                    Assert.Equal($"base-{other.Name}", other.GetValue(merged));
            }
        }

        /// <summary>
        /// The writable string properties a merged <see cref="ProgramItem"/> must carry, i.e. every
        /// one except the identity the two layers share.
        /// </summary>
        private static PropertyInfo[] MergedProgramItemProperties()
            => typeof(ProgramItem)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite)
                .Where(p => p.Name is not (nameof(ProgramItem.ProgId) or nameof(ProgramItem.Key)))
                .ToArray();

        // ---- PluginSettings：per progId 相加 ----

        [Fact]
        [DisplayName("plugin：兩層皆有時相加，套裝在前、客製在後")]
        public void GetPluginTypes_BothLayers_ConcatenatesBaseThenCustomize()
        {
            var @base = Plugins(("Order", s_pkgChain));
            var cust = Plugins(("Order", s_custChain));

            Assert.Equal(s_concatenated, CustomizeOverlay.GetPluginTypes(cust, @base, "Order"));
        }

        [Fact]
        [DisplayName("plugin：只有一層宣告時回該層的鏈")]
        public void GetPluginTypes_SingleLayer_ReturnsThatChain()
        {
            var @base = Plugins(("Order", s_pkgAuditOnly));
            var cust = Plugins(("Customer", s_custDedupeOnly));

            Assert.Equal(s_pkgAuditOnly, CustomizeOverlay.GetPluginTypes(cust, @base, "Order"));
            Assert.Equal(s_custDedupeOnly, CustomizeOverlay.GetPluginTypes(cust, @base, "Customer"));
        }

        [Fact]
        [DisplayName("plugin：兩層皆無該 progId 或皆為 null 時回空集合")]
        public void GetPluginTypes_BothMiss_ReturnsEmpty()
        {
            Assert.Empty(CustomizeOverlay.GetPluginTypes(Plugins(), Plugins(), "Nope"));
            Assert.Empty(CustomizeOverlay.GetPluginTypes(null, null, "Order"));
        }

        [Fact]
        [DisplayName("plugin：客製無法停用套裝的 plugin——沒有 tombstone，相加是唯一語意")]
        public void GetPluginTypes_CustomizeCannotSuppressBase()
        {
            var @base = Plugins(("Order", s_pkgAuditOnly));
            var cust = Plugins(("Order", []));

            // 客製宣告了該 progId 但鏈為空，套裝的 plugin 仍然在。
            Assert.Equal(s_pkgAuditOnly, CustomizeOverlay.GetPluginTypes(cust, @base, "Order"));
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

        private static PluginSettings Plugins(params (string ProgId, string[] Types)[] items)
        {
            var settings = new PluginSettings();
            foreach (var (progId, types) in items)
            {
                var program = settings.Items!.Add(progId);
                foreach (var type in types)
                    program.Plugins!.Add(type);
            }
            return settings;
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
