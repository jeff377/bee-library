using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Identity
{
    /// <summary>
    /// CompanyInfo 數值位數解析（公司覆寫 vs 框架預設）與 CompanyNumberFormats 三棲 round-trip。
    /// </summary>
    public class CompanyInfoTests
    {
        [Fact]
        [DisplayName("GetDecimals 公司覆寫存在時應回覆寫值")]
        public void GetDecimals_Override_ReturnsOverride()
        {
            var company = new CompanyInfo
            {
                CompanyId = "C001",
                NumberFormats = [new NumberFormatItem(NumberKind.Percent, 4)],
            };

            Assert.Equal(4, company.GetDecimals(NumberKind.Percent));
        }

        [Fact]
        [DisplayName("GetDecimals 無公司覆寫時應退框架預設位數")]
        public void GetDecimals_NoOverride_ReturnsFrameworkDefault()
        {
            var company = new CompanyInfo { CompanyId = "C001" };

            // 框架預設：Amount=2、UnitPrice=4、Weight=3
            Assert.Equal(2, company.GetDecimals(NumberKind.Amount));
            Assert.Equal(4, company.GetDecimals(NumberKind.UnitPrice));
            Assert.Equal(3, company.GetDecimals(NumberKind.Weight));
        }

        [Fact]
        [DisplayName("GetDecimals 部分覆寫時應只覆寫指定 kind、其餘退框架預設")]
        public void GetDecimals_PartialOverride_OthersFallBack()
        {
            var company = new CompanyInfo
            {
                CompanyId = "C001",
                NumberFormats = [new NumberFormatItem(NumberKind.UnitPrice, 6)],
            };

            Assert.Equal(6, company.GetDecimals(NumberKind.UnitPrice));   // 覆寫
            Assert.Equal(2, company.GetDecimals(NumberKind.Percent));     // 框架預設
        }

        [Fact]
        [DisplayName("FindDecimals 命中回位數、未命中回 null")]
        public void FindDecimals_HitAndMiss()
        {
            CompanyNumberFormats formats = [new NumberFormatItem(NumberKind.Cost, 5)];

            Assert.Equal(5, formats.FindDecimals(NumberKind.Cost));
            Assert.Null(formats.FindDecimals(NumberKind.Percent));
        }

        [Fact]
        [DisplayName("CompanyNumberFormats XML 序列化應正確還原覆寫項")]
        public void CompanyNumberFormats_XmlRoundtrip_PreservesItems()
        {
            CompanyNumberFormats original =
            [
                new NumberFormatItem(NumberKind.Percent, 3),
                new NumberFormatItem(NumberKind.UnitPrice, 6),
            ];

            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<CompanyNumberFormats>(xml);

            Assert.NotNull(restored);
            Assert.Equal(2, restored!.Count);
            Assert.Equal(3, restored.FindDecimals(NumberKind.Percent));
            Assert.Equal(6, restored.FindDecimals(NumberKind.UnitPrice));
        }

        [Fact]
        [DisplayName("CompanyNumberFormats JSON 序列化應正確還原覆寫項")]
        public void CompanyNumberFormats_JsonRoundtrip_PreservesItems()
        {
            CompanyNumberFormats original = [new NumberFormatItem(NumberKind.Amount, 0)];

            var json = JsonCodec.Serialize(original);
            var restored = JsonCodec.Deserialize<CompanyNumberFormats>(json);

            Assert.NotNull(restored);
            Assert.Single(restored!);
            Assert.Equal(0, restored.FindDecimals(NumberKind.Amount));
        }

        // --- 多幣別：本幣 / 現金捨入 / 可用幣別 ---

        private static CurrencySettings BuildCurrencies() =>
        [
            new CurrencyItem("USD", 0.01m, "$", "US Dollar"),
            new CurrencyItem("JPY", 1m, "¥", "Japanese Yen"),
            new CurrencyItem("CHF", 0.01m, "CHF", "Swiss Franc"),
        ];

        [Fact]
        [DisplayName("GetCashRounding 公司覆寫存在時回覆寫單位（CHF→0.05）")]
        public void GetCashRounding_Override_ReturnsOverride()
        {
            var currencies = BuildCurrencies();
            var company = new CompanyInfo
            {
                CompanyId = "C001",
                CashRounding = [new CashRoundingItem("CHF", 0.05m)],
            };

            Assert.Equal(0.05m, company.GetCashRounding("CHF", currencies));
        }

        [Fact]
        [DisplayName("GetCashRounding 無公司覆寫時退幣別自然最小單位")]
        public void GetCashRounding_NoOverride_ReturnsCurrencyNaturalUnit()
        {
            var currencies = BuildCurrencies();
            var company = new CompanyInfo { CompanyId = "C001" };

            Assert.Equal(0.01m, company.GetCashRounding("USD", currencies));
            Assert.Equal(1m, company.GetCashRounding("JPY", currencies));
        }

        [Fact]
        [DisplayName("GetAllowedCurrencies 白名單非空時回子集")]
        public void GetAllowedCurrencies_NonEmpty_ReturnsSubset()
        {
            var currencies = BuildCurrencies();
            var company = new CompanyInfo
            {
                CompanyId = "C001",
                AllowedCurrencies = [new AllowedCurrencyItem("USD"), new AllowedCurrencyItem("JPY")],
            };

            Assert.Equal(["USD", "JPY"], company.GetAllowedCurrencies(currencies));
        }

        [Fact]
        [DisplayName("GetAllowedCurrencies 白名單空時回全系統幣別碼")]
        public void GetAllowedCurrencies_Empty_ReturnsAllSystemCodes()
        {
            var currencies = BuildCurrencies();
            var company = new CompanyInfo { CompanyId = "C001" };

            Assert.Equal(["USD", "JPY", "CHF"], company.GetAllowedCurrencies(currencies));
        }

        [Fact]
        [DisplayName("CompanyInfo 多幣別欄位 XML round-trip 應保留本幣/現金捨入/白名單")]
        public void CompanyInfo_MultiCurrencyFields_XmlRoundtrip()
        {
            var original = new CompanyInfo
            {
                CompanyId = "C001",
                DefaultCurrency = "USD",
                CashRounding = [new CashRoundingItem("CHF", 0.05m)],
                AllowedCurrencies = [new AllowedCurrencyItem("USD"), new AllowedCurrencyItem("JPY")],
            };

            var xml = XmlCodec.Serialize(original);
            var restored = XmlCodec.Deserialize<CompanyInfo>(xml);

            Assert.NotNull(restored);
            Assert.Equal("USD", restored!.DefaultCurrency);
            Assert.Equal(0.05m, restored.CashRounding.FindUnit("CHF"));
            Assert.Equal(2, restored.AllowedCurrencies.Count);
        }

        [Fact]
        [DisplayName("CompanyCashRounding.FindUnit 命中回單位、未命中回 null")]
        public void CompanyCashRounding_FindUnit_HitAndMiss()
        {
            CompanyCashRounding rounding = [new CashRoundingItem("CHF", 0.05m)];

            Assert.Equal(0.05m, rounding.FindUnit("CHF"));
            Assert.Null(rounding.FindUnit("USD"));
        }
    }
}
