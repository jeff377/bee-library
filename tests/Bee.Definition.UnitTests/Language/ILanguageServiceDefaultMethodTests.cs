using System.ComponentModel;
using Bee.Definition.Language;

namespace Bee.Definition.UnitTests.Language
{
    /// <summary>
    /// 驗證 <see cref="ILanguageService"/> 四個帶 <c>customizeId</c> 參數的預設介面方法，
    /// 在實作類別未覆寫時，正確委派至對應的不帶 <c>customizeId</c> 的多載。
    /// 使用僅實作必要抽象成員的最小化 stub，使 default 方法路徑得以執行。
    /// </summary>
    public class ILanguageServiceDefaultMethodTests
    {
        private sealed class BaseOnlyLanguageService : ILanguageService
        {
            public string GetLangText(string lang, string fullKey) => $"fullkey:{fullKey}";
            public string GetLangText(string lang, string @namespace, string subKey) => $"ns:{@namespace}.{subKey}";
            public bool TryGetLangText(string lang, string fullKey, out string text) { text = $"try:{fullKey}"; return true; }
            public bool TryGetLangText(string lang, string @namespace, string subKey, out string text) { text = $"try:{@namespace}.{subKey}"; return true; }
            public LanguageEnum? GetLangEnum(string lang, string fullName) => null;
            public LanguageEnum? GetLangEnum(string lang, string @namespace, string enumName) => null;
            public string? GetLangEnumText(string lang, string fullName, string code) => $"enum:{code}";
        }

        [Fact]
        [DisplayName("GetLangText 帶 customizeId 預設實作應委派至三參數多載")]
        public void GetLangText_FourArgDefault_DelegatesToThreeArgOverload()
        {
            ILanguageService svc = new BaseOnlyLanguageService();
            var result = svc.GetLangText("cust", "zh-TW", "Common", "OK");
            Assert.Equal("ns:Common.OK", result);
        }

        [Fact]
        [DisplayName("TryGetLangText 帶 customizeId 預設實作應委派至四參數多載")]
        public void TryGetLangText_FiveArgDefault_DelegatesToFourArgOverload()
        {
            ILanguageService svc = new BaseOnlyLanguageService();
            var hit = svc.TryGetLangText("cust", "zh-TW", "Common", "OK", out string text);
            Assert.True(hit);
            Assert.Equal("try:Common.OK", text);
        }

        [Fact]
        [DisplayName("GetLangEnum 帶 customizeId 預設實作應委派至三參數多載")]
        public void GetLangEnum_FourArgDefault_DelegatesToThreeArgOverload()
        {
            ILanguageService svc = new BaseOnlyLanguageService();
            var result = svc.GetLangEnum("cust", "zh-TW", "Common", "Gender");
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("GetLangEnumText 帶 customizeId 預設實作應委派至三參數多載")]
        public void GetLangEnumText_FourArgDefault_DelegatesToThreeArgOverload()
        {
            ILanguageService svc = new BaseOnlyLanguageService();
            var result = svc.GetLangEnumText("cust", "zh-TW", "Common.Gender", "M");
            Assert.Equal("enum:M", result);
        }
    }
}
