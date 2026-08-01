using System.ComponentModel;
using Bee.Api.Client.Definitions;
using Bee.Definition.Language;

namespace Bee.Api.Client.UnitTests.Definitions
{
    /// <summary>
    /// <see cref="SnapshotLanguageService"/> 測試：用戶端在已取回的兩層快照上做選用與預設語系
    /// fallback，行為必須與伺服端的 <c>LanguageService</c> 一致（兩者都走 <c>CustomizeOverlay</c>）。
    /// </summary>
    public class SnapshotLanguageServiceTests
    {
        [Fact]
        [DisplayName("客製有該 key 時取客製值")]
        public void GetLangText_CustomizeHasKey_ReturnsCustomizeValue()
        {
            var svc = Build(("zh-TW", "Common", Res(("OK", "確定")), Res(("OK", "送出"))));

            Assert.Equal("送出", svc.GetLangText("zh-TW", "Common", "OK"));
        }

        [Fact]
        [DisplayName("客製沒有該 key 時延用套裝值")]
        public void GetLangText_CustomizeMissesKey_FallsBackToBase()
        {
            var svc = Build(("zh-TW", "Common", Res(("OK", "確定"), ("Cancel", "取消")), Res(("OK", "送出"))));

            Assert.Equal("取消", svc.GetLangText("zh-TW", "Common", "Cancel"));
        }

        [Fact]
        [DisplayName("要求語系查無時應退到預設語系")]
        public void GetLangText_MissingInRequestedLang_FallsBackToDefaultLang()
        {
            var svc = Build(
                defaultLang: "en-US",
                ("zh-TW", "Common", Res(), null),
                ("en-US", "Common", Res(("OK", "OK")), null));

            Assert.Equal("OK", svc.GetLangText("zh-TW", "Common", "OK"));
        }

        [Fact]
        [DisplayName("預設語系的客製層同樣參與 fallback")]
        public void GetLangText_DefaultLangFallback_AlsoOverlaysCustomize()
        {
            var svc = Build(
                defaultLang: "en-US",
                ("zh-TW", "Common", Res(), null),
                ("en-US", "Common", Res(("OK", "OK")), Res(("OK", "Submit"))));

            Assert.Equal("Submit", svc.GetLangText("zh-TW", "Common", "OK"));
        }

        [Fact]
        [DisplayName("兩層與 fallback 皆查無時回 fullKey（與伺服端同一個最後手段）")]
        public void GetLangText_AllMiss_ReturnsFullKey()
        {
            var svc = Build(("zh-TW", "Common", Res(), null));

            Assert.Equal("Common.Nope", svc.GetLangText("zh-TW", "Common", "Nope"));
        }

        [Fact]
        [DisplayName("快照沒有該 namespace 時不丟例外，視同查無")]
        public void GetLangText_NamespaceNotInSnapshot_TreatedAsMiss()
        {
            var svc = Build(("zh-TW", "Common", Res(("OK", "確定")), null));

            Assert.False(svc.TryGetLangText("zh-TW", "Unknown", "OK", out _));
            Assert.Equal("Unknown.OK", svc.GetLangText("zh-TW", "Unknown", "OK"));
        }

        [Fact]
        [DisplayName("fullKey 多載於第一個點切開")]
        public void GetLangText_FullKey_SplitsOnFirstDot()
        {
            var svc = Build(("zh-TW", "Customer", Res(("Field.sys_name.Caption", "客戶名稱")), null));

            Assert.Equal("客戶名稱", svc.GetLangText("zh-TW", "Customer.Field.sys_name.Caption"));
        }

        [Fact]
        [DisplayName("Enum：客製有同名 enum 時整組取代")]
        public void GetLangEnum_CustomizeHasEnum_ReplacesWholeSet()
        {
            var svc = Build(("zh-TW", "Common",
                ResEnum("Gender", ("M", "男"), ("F", "女")),
                ResEnum("Gender", ("M", "先生"))));

            var result = svc.GetLangEnum("zh-TW", "Common", "Gender");

            Assert.NotNull(result);
            Assert.Single(result!.Entries);
            Assert.Equal("先生", result.GetText("M"));
        }

        [Fact]
        [DisplayName("Enum：要求語系查無時應退到預設語系")]
        public void GetLangEnum_MissingInRequestedLang_FallsBackToDefaultLang()
        {
            var svc = Build(
                defaultLang: "en-US",
                ("zh-TW", "Common", Res(), null),
                ("en-US", "Common", ResEnum("Gender", ("M", "Male")), null));

            Assert.Equal("Male", svc.GetLangEnum("zh-TW", "Common", "Gender")!.GetText("M"));
        }

        // ---- Fixtures ----

        private static SnapshotLanguageService Build(
            params (string Lang, string Ns, LanguageResource? Base, LanguageResource? Customize)[] entries)
            => Build(string.Empty, entries);

        private static SnapshotLanguageService Build(
            string defaultLang,
            params (string Lang, string Ns, LanguageResource? Base, LanguageResource? Customize)[] entries)
        {
            var snapshot = new Dictionary<string, LanguageLayers>(StringComparer.Ordinal);
            foreach (var (lang, ns, @base, customize) in entries)
                snapshot[SnapshotLanguageService.BuildKey(lang, ns)] = new LanguageLayers(@base, customize);
            return new SnapshotLanguageService(snapshot, defaultLang);
        }

        private static LanguageResource Res(params (string Key, string Value)[] items)
        {
            var resource = new LanguageResource { Namespace = "Common", Lang = "zh-TW" };
            foreach (var (key, value) in items)
                resource.Items.Add(key, value);
            return resource;
        }

        private static LanguageResource ResEnum(string enumName, params (string Code, string Text)[] entries)
        {
            var resource = Res();
            var langEnum = new LanguageEnum { Name = enumName };
            foreach (var (code, text) in entries)
                langEnum.Entries.Add(code, text);
            resource.Enums.Add(langEnum);
            return resource;
        }
    }
}
