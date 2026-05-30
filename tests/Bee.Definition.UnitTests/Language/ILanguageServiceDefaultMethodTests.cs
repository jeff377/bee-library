using System.ComponentModel;
using Bee.Definition.Language;

namespace Bee.Definition.UnitTests.Language
{
    /// <summary>
    /// 驗證 <see cref="ILanguageService"/> 四個含 customizeId 的預設介面方法
    /// 確實委派至對應的基礎多載，而非空實作。
    /// <c>MinimalLanguageService</c> 不 override 這四個方法，強制走 default interface method。
    /// </summary>
    public class ILanguageServiceDefaultMethodTests
    {
        private sealed class MinimalLanguageService : ILanguageService
        {
            public string GetLangText(string lang, string fullKey) => fullKey;

            public string GetLangText(string lang, string @namespace, string subKey)
                => $"{@namespace}.{subKey}";

            public bool TryGetLangText(string lang, string fullKey, out string text)
            {
                text = fullKey;
                return true;
            }

            public bool TryGetLangText(string lang, string @namespace, string subKey, out string text)
            {
                text = $"{@namespace}.{subKey}";
                return true;
            }

            public LanguageEnum? GetLangEnum(string lang, string fullName)
            {
                int dot = fullName.IndexOf('.');
                if (dot < 0) return null;
                return GetLangEnum(lang, fullName[..dot], fullName[(dot + 1)..]);
            }

            public LanguageEnum? GetLangEnum(string lang, string @namespace, string enumName)
            {
                var e = new LanguageEnum { Name = enumName };
                e.Entries.Add("M", "男");
                return e;
            }

            public string? GetLangEnumText(string lang, string fullName, string code)
            {
                int dot = fullName.IndexOf('.');
                if (dot < 0) return null;
                var e = GetLangEnum(lang, fullName[..dot], fullName[(dot + 1)..]);
                return e?.GetText(code);
            }

            // 不 override 以下四個 customizeId 多載 → 走 default interface method
        }

        [Fact]
        [DisplayName("TryGetLangText 含 customizeId 預設介面實作應委派至不含 customizeId 的三參數多載")]
        public void TryGetLangText_WithCustomizeId_DelegatesToBaseOverload()
        {
            ILanguageService service = new MinimalLanguageService();

            bool result = service.TryGetLangText("acme", "zh-TW", "Common", "OK", out string text);

            Assert.True(result);
            Assert.Equal("Common.OK", text);
        }

        [Fact]
        [DisplayName("GetLangText 含 customizeId 預設介面實作應委派至不含 customizeId 的三參數多載")]
        public void GetLangText_WithCustomizeId_DelegatesToBaseOverload()
        {
            ILanguageService service = new MinimalLanguageService();

            string result = service.GetLangText("acme", "zh-TW", "Common", "OK");

            Assert.Equal("Common.OK", result);
        }

        [Fact]
        [DisplayName("GetLangEnum 含 customizeId 預設介面實作應委派至不含 customizeId 的三參數多載")]
        public void GetLangEnum_WithCustomizeId_DelegatesToBaseOverload()
        {
            ILanguageService service = new MinimalLanguageService();

            LanguageEnum? result = service.GetLangEnum("acme", "zh-TW", "Common", "Gender");

            Assert.NotNull(result);
            Assert.Equal("Gender", result!.Name);
        }

        [Fact]
        [DisplayName("GetLangEnumText 含 customizeId 預設介面實作應委派至不含 customizeId 的基礎多載")]
        public void GetLangEnumText_WithCustomizeId_DelegatesToBaseOverload()
        {
            ILanguageService service = new MinimalLanguageService();

            string? result = service.GetLangEnumText("acme", "zh-TW", "Common.Gender", "M");

            Assert.Equal("男", result);
        }
    }
}
