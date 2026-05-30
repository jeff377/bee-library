using System.ComponentModel;
using Bee.Definition.Language;

namespace Bee.Definition.UnitTests.Language
{
    /// <summary>
    /// 驗證 <see cref="ILanguageService"/> 四個預設介面實作（customizeId 多載）正確委派至對應的基底多載。
    /// 使用不覆寫任何預設成員的最小實作，確保預設方法本體被執行（程式碼覆蓋率）。
    /// </summary>
    public class ILanguageServiceDefaultImplTests
    {
        // Minimal implementation — only the abstract (non-default) members are implemented.
        // The four customizeId default methods are intentionally left as interface defaults
        // so their bodies execute when called through an ILanguageService reference.
        private sealed class MinimalLanguageService : ILanguageService
        {
            public string GetLangText(string lang, string fullKey) => $"fullkey:{fullKey}";
            public string GetLangText(string lang, string @namespace, string subKey) => $"{@namespace}.{subKey}:result";

            public bool TryGetLangText(string lang, string fullKey, out string text)
            {
                text = $"fullkey:{fullKey}";
                return true;
            }

            public bool TryGetLangText(string lang, string @namespace, string subKey, out string text)
            {
                text = $"{@namespace}.{subKey}:result";
                return true;
            }

            public LanguageEnum? GetLangEnum(string lang, string fullName) => null;
            public LanguageEnum? GetLangEnum(string lang, string @namespace, string enumName) => null;
            public string? GetLangEnumText(string lang, string fullName, string code) => $"{fullName}:{code}";
        }

        [Fact]
        [DisplayName("ILanguageService 預設 GetLangText(customizeId) 應委派至三參數多載")]
        public void DefaultGetLangText_WithCustomizeId_DelegatesToThreeArgOverload()
        {
            ILanguageService svc = new MinimalLanguageService();

            var result = svc.GetLangText("acme", "zh-TW", "Common", "OK");

            Assert.Equal("Common.OK:result", result);
        }

        [Fact]
        [DisplayName("ILanguageService 預設 TryGetLangText(customizeId) 應委派至三參數多載")]
        public void DefaultTryGetLangText_WithCustomizeId_DelegatesToThreeArgOverload()
        {
            ILanguageService svc = new MinimalLanguageService();

            var found = svc.TryGetLangText("acme", "zh-TW", "Common", "OK", out var text);

            Assert.True(found);
            Assert.Equal("Common.OK:result", text);
        }

        [Fact]
        [DisplayName("ILanguageService 預設 GetLangEnum(customizeId) 應委派至三參數多載")]
        public void DefaultGetLangEnum_WithCustomizeId_DelegatesToThreeArgOverload()
        {
            ILanguageService svc = new MinimalLanguageService();

            var result = svc.GetLangEnum("acme", "zh-TW", "Common", "Gender");

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("ILanguageService 預設 GetLangEnumText(customizeId) 應委派至三參數多載")]
        public void DefaultGetLangEnumText_WithCustomizeId_DelegatesToThreeArgOverload()
        {
            ILanguageService svc = new MinimalLanguageService();

            var result = svc.GetLangEnumText("acme", "zh-TW", "Common.Gender", "M");

            Assert.Equal("Common.Gender:M", result);
        }
    }
}
