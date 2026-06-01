using System.ComponentModel;
using Bee.Definition.Language;

namespace Bee.Definition.UnitTests.Language
{
    /// <summary>
    /// 驗證 <see cref="ILanguageService"/> 四個預設介面實作的委派行為。
    /// 使用不覆寫這四個預設方法的最小實作類別，透過介面型別呼叫以觸發預設實作路徑。
    /// </summary>
    public class ILanguageServiceDefaultImplTests
    {
        private sealed class MinimalLanguageService : ILanguageService
        {
            public string GetLangText(string lang, string fullKey)
                => $"{lang}:{fullKey}";

            public string GetLangText(string lang, string @namespace, string subKey)
                => $"{lang}:{@namespace}.{subKey}";

            public bool TryGetLangText(string lang, string fullKey, out string text)
            {
                text = $"found:{fullKey}";
                return true;
            }

            public bool TryGetLangText(string lang, string @namespace, string subKey, out string text)
            {
                text = $"found:{@namespace}.{subKey}";
                return true;
            }

            public LanguageEnum? GetLangEnum(string lang, string fullName)
                => new() { Name = fullName };

            public LanguageEnum? GetLangEnum(string lang, string @namespace, string enumName)
                => new() { Name = enumName };

            public string? GetLangEnumText(string lang, string fullName, string code)
                => $"{fullName}:{code}";
        }

        [Fact]
        [DisplayName("ILanguageService 預設實作 GetLangText(4參數) 應委派給 GetLangText(3參數)")]
        public void GetLangText_DefaultImpl4Args_DelegatesTo3ArgOverload()
        {
            ILanguageService svc = new MinimalLanguageService();
            var result = svc.GetLangText("cust", "zh-TW", "Common", "OK");
            Assert.Equal("zh-TW:Common.OK", result);
        }

        [Fact]
        [DisplayName("ILanguageService 預設實作 TryGetLangText(5參數) 應委派給 TryGetLangText(4參數)")]
        public void TryGetLangText_DefaultImpl5Args_DelegatesTo4ArgOverload()
        {
            ILanguageService svc = new MinimalLanguageService();
            bool hit = svc.TryGetLangText("cust", "zh-TW", "Common", "OK", out string text);
            Assert.True(hit);
            Assert.Equal("found:Common.OK", text);
        }

        [Fact]
        [DisplayName("ILanguageService 預設實作 GetLangEnum(4參數) 應委派給 GetLangEnum(3參數)")]
        public void GetLangEnum_DefaultImpl4Args_DelegatesTo3ArgOverload()
        {
            ILanguageService svc = new MinimalLanguageService();
            var result = svc.GetLangEnum("cust", "zh-TW", "Common", "Gender");
            Assert.NotNull(result);
            Assert.Equal("Gender", result!.Name);
        }

        [Fact]
        [DisplayName("ILanguageService 預設實作 GetLangEnumText(4參數) 應委派給 GetLangEnumText(3參數)")]
        public void GetLangEnumText_DefaultImpl4Args_DelegatesTo3ArgOverload()
        {
            ILanguageService svc = new MinimalLanguageService();
            var result = svc.GetLangEnumText("cust", "zh-TW", "Common.Gender", "M");
            Assert.Equal("Common.Gender:M", result);
        }
    }
}
