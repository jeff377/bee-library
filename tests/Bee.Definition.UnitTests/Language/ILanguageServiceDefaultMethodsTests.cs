using System.ComponentModel;
using Bee.Definition.Language;

namespace Bee.Definition.UnitTests.Language
{
    /// <summary>
    /// 補強 <see cref="ILanguageService"/> 四個 default interface methods 的覆蓋率。
    /// 這四個 default methods 在任何未 override 它們的實作中生效，此處透過最小 stub 驗證
    /// 它們正確委派至對應的基底多載（忽略 customizeId 參數）。
    /// </summary>
    public class ILanguageServiceDefaultMethodsTests
    {
        // ---- Minimal stub — 實作所有抽象成員，不 override 任何 default method ---

        private sealed class StubLanguageService : ILanguageService
        {
            private readonly Dictionary<string, string> _texts = [];
            private readonly Dictionary<string, LanguageEnum?> _enums = [];

            public void AddText(string lang, string ns, string subKey, string value)
                => _texts[$"{lang}.{ns}.{subKey}"] = value;

            public void AddEnum(string lang, string ns, string enumName, LanguageEnum langEnum)
                => _enums[$"{lang}.{ns}.{enumName}"] = langEnum;

            public string GetLangText(string lang, string fullKey)
            {
                int dot = fullKey.IndexOf('.');
                if (dot < 0) return fullKey;
                return GetLangText(lang, fullKey[..dot], fullKey[(dot + 1)..]);
            }

            public string GetLangText(string lang, string @namespace, string subKey)
                => _texts.TryGetValue($"{lang}.{@namespace}.{subKey}", out var v)
                    ? v
                    : $"{@namespace}.{subKey}";

            public bool TryGetLangText(string lang, string fullKey, out string text)
            {
                int dot = fullKey.IndexOf('.');
                if (dot < 0) { text = string.Empty; return false; }
                return TryGetLangText(lang, fullKey[..dot], fullKey[(dot + 1)..], out text);
            }

            public bool TryGetLangText(string lang, string @namespace, string subKey, out string text)
            {
                if (_texts.TryGetValue($"{lang}.{@namespace}.{subKey}", out var v))
                {
                    text = v;
                    return true;
                }
                text = string.Empty;
                return false;
            }

            public LanguageEnum? GetLangEnum(string lang, string fullName)
            {
                int dot = fullName.IndexOf('.');
                if (dot < 0) return null;
                return GetLangEnum(lang, fullName[..dot], fullName[(dot + 1)..]);
            }

            public LanguageEnum? GetLangEnum(string lang, string @namespace, string enumName)
                => _enums.TryGetValue($"{lang}.{@namespace}.{enumName}", out var e) ? e : null;

            public string? GetLangEnumText(string lang, string fullName, string code)
                => GetLangEnum(lang, fullName)?.GetText(code);
        }

        // ---- Tests ---

        [Fact]
        [DisplayName("ILanguageService 預設 GetLangText(customizeId,...) 忽略 customizeId 委派 GetLangText(lang,ns,subKey)")]
        public void GetLangText_DefaultMethod_IgnoresCustomizeIdDelegatesToBase()
        {
            var stub = new StubLanguageService();
            stub.AddText("zh-TW", "Common", "OK", "確定");
            ILanguageService svc = stub;

            string result = svc.GetLangText("acme", "zh-TW", "Common", "OK");

            Assert.Equal("確定", result);
        }

        [Fact]
        [DisplayName("ILanguageService 預設 TryGetLangText(customizeId,...) 命中時回傳 true 並輸出正確文字")]
        public void TryGetLangText_DefaultMethod_HitReturnsTextAndTrue()
        {
            var stub = new StubLanguageService();
            stub.AddText("zh-TW", "Common", "Cancel", "取消");
            ILanguageService svc = stub;

            bool found = svc.TryGetLangText("acme", "zh-TW", "Common", "Cancel", out string text);

            Assert.True(found);
            Assert.Equal("取消", text);
        }

        [Fact]
        [DisplayName("ILanguageService 預設 GetLangEnum(customizeId,...) 委派 GetLangEnum(lang,ns,enumName) 並回傳正確 enum")]
        public void GetLangEnum_DefaultMethod_DelegatesToBase()
        {
            var langEnum = new LanguageEnum { Name = "Gender" };
            langEnum.Entries.Add("M", "男");
            var stub = new StubLanguageService();
            stub.AddEnum("zh-TW", "Common", "Gender", langEnum);
            ILanguageService svc = stub;

            var result = svc.GetLangEnum("acme", "zh-TW", "Common", "Gender");

            Assert.NotNull(result);
            Assert.Equal("男", result!.GetText("M"));
        }

        [Fact]
        [DisplayName("ILanguageService 預設 GetLangEnumText(customizeId,...) 委派 GetLangEnumText(lang,fullName,code) 並回傳正確文字")]
        public void GetLangEnumText_DefaultMethod_DelegatesToBase()
        {
            var langEnum = new LanguageEnum { Name = "Gender" };
            langEnum.Entries.Add("F", "女");
            var stub = new StubLanguageService();
            stub.AddEnum("zh-TW", "Common", "Gender", langEnum);
            ILanguageService svc = stub;

            string? result = svc.GetLangEnumText("acme", "zh-TW", "Common.Gender", "F");

            Assert.Equal("女", result);
        }
    }
}
