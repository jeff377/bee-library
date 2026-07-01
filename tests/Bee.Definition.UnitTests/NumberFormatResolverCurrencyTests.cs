using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// NumberFormatResolver 多幣別：依 CUKY 欄幣別解析位數/捨入、兩層捨入（明細 round-then-sum +
    /// 最終現金捨入）、本幣金額、preserve。
    /// </summary>
    public class NumberFormatResolverCurrencyTests
    {
        private static CurrencySettings Currencies() =>
        [
            new CurrencyItem("USD", 0.01m, "$", "US Dollar"),
            new CurrencyItem("JPY", 1m, "¥", "Japanese Yen"),
            new CurrencyItem("BHD", 0.001m, "BD", "Bahraini Dinar"),
            new CurrencyItem("CHF", 0.01m, "CHF", "Swiss Franc"),
        ];

        private static RoundingContext Ctx(CompanyInfo? company = null) =>
            new() { Company = company, CurrencySettings = Currencies() };

        [Theory]
        [InlineData("USD", 2)]
        [InlineData("JPY", 0)]
        [InlineData("BHD", 3)]
        [DisplayName("ResolveDecimals 金額欄依 CUKY 欄幣別給不同位數")]
        public void ResolveDecimals_Amount_ByCurrency(string code, int expected)
        {
            Assert.Equal(expected, NumberFormatResolver.ResolveDecimals(NumberKind.Amount, Ctx(), code));
        }

        [Theory]
        [InlineData("USD", "N2")]
        [InlineData("JPY", "N0")]
        [InlineData("BHD", "N3")]
        [DisplayName("ResolveFormat 金額欄依幣別給不同格式字串")]
        public void ResolveFormat_Amount_ByCurrency(string code, string expected)
        {
            Assert.Equal(expected, NumberFormatResolver.ResolveFormat(NumberKind.Amount, Ctx(), code));
        }

        [Fact]
        [DisplayName("ResolveDecimals refCode 空時退公司本幣，再退框架 2 位")]
        public void ResolveDecimals_EmptyRefCode_FallsBackToDefaultCurrencyThenFramework()
        {
            var company = new CompanyInfo { CompanyId = "C001", DefaultCurrency = "JPY" };

            // 空 refCode → 退公司本幣 JPY（0 位）
            Assert.Equal(0, NumberFormatResolver.ResolveDecimals(NumberKind.Amount, Ctx(company), null));

            // 無公司本幣、無 refCode → 框架 2 位
            Assert.Equal(2, NumberFormatResolver.ResolveDecimals(NumberKind.Amount, Ctx(), null));
        }

        [Fact]
        [DisplayName("ResolveDecimals 無幣別主檔時金額退框架 2 位")]
        public void ResolveDecimals_NoCurrencyMaster_FrameworkDefault()
        {
            var ctx = new RoundingContext { Company = null, CurrencySettings = null };

            Assert.Equal(2, NumberFormatResolver.ResolveDecimals(NumberKind.Amount, ctx, "USD"));
        }

        [Fact]
        [DisplayName("RoundByKind 金額依幣別捨入（USD 2 位 / JPY 0 位）")]
        public void RoundByKind_Amount_ByCurrency()
        {
            Assert.Equal(12.35m, NumberFormatResolver.RoundByKind(12.345m, NumberKind.Amount, Ctx(), "USD"));
            Assert.Equal(12m, NumberFormatResolver.RoundByKind(12.4m, NumberKind.Amount, Ctx(), "JPY"));
            Assert.Equal(13m, NumberFormatResolver.RoundByKind(12.5m, NumberKind.Amount, Ctx(), "JPY"));
        }

        [Fact]
        [DisplayName("round-then-sum 不變式：明細各捨幣別位數後加總 == 表頭合計，且 ≠ 全精度後捨（USD）")]
        public void RoundThenSum_Usd_InvariantHolds()
        {
            decimal[] details = [10.333m, 10.333m, 10.333m];
            var ctx = Ctx();

            decimal total = 0m;
            foreach (var d in details)
                total += NumberFormatResolver.RoundByKind(d, NumberKind.Amount, ctx, "USD");

            decimal sumThenRound = NumberFormatResolver.RoundByKind(
                details[0] + details[1] + details[2], NumberKind.Amount, ctx, "USD");

            Assert.Equal(30.99m, total);        // 10.33 × 3
            Assert.Equal(31.00m, sumThenRound); // 30.999 → 31.00
            Assert.NotEqual(total, sumThenRound);
        }

        [Fact]
        [DisplayName("round-then-sum：JPY 0 位明細加總，位數與 USD 不同")]
        public void RoundThenSum_Jpy_ZeroDecimals()
        {
            decimal[] details = [100.4m, 100.4m, 100.4m];
            var ctx = Ctx();

            decimal total = 0m;
            foreach (var d in details)
                total += NumberFormatResolver.RoundByKind(d, NumberKind.Amount, ctx, "JPY");

            Assert.Equal(300m, total); // 各捨為 100 → 300
        }

        [Fact]
        [DisplayName("RoundCash 公司對 CHF 設 0.05 → 捨到 5 分倍數，diff = payable − total")]
        public void RoundCash_CompanyOverride_RoundsToUnit_WithDiff()
        {
            var company = new CompanyInfo
            {
                CompanyId = "C001",
                CashRounding = [new CashRoundingItem("CHF", 0.05m)],
            };
            var ctx = Ctx(company);

            decimal total = 12.34m;
            decimal payable = NumberFormatResolver.RoundCash(total, "CHF", ctx);
            Assert.Equal(12.35m, payable);
            Assert.Equal(0.01m, payable - total);

            decimal total2 = 12.32m;
            decimal payable2 = NumberFormatResolver.RoundCash(total2, "CHF", ctx);
            Assert.Equal(12.30m, payable2);
            Assert.Equal(-0.02m, payable2 - total2);
        }

        [Fact]
        [DisplayName("RoundCash 無公司覆寫 → 退幣別自然單位（USD 0.01，等同不額外捨入）")]
        public void RoundCash_NoOverride_NoExtraRounding()
        {
            var ctx = Ctx(new CompanyInfo { CompanyId = "C001" });

            // USD 自然單位 0.01；total 已在 2 位 → payable == total、diff = 0
            Assert.Equal(12.34m, NumberFormatResolver.RoundCash(12.34m, "USD", ctx));
        }

        [Fact]
        [DisplayName("本幣金額：home_amount = round(amount × rate, Amount, 本幣)；本幣 JPY 0 位")]
        public void HomeAmount_ConvertedAndRoundedToHomeCurrency()
        {
            var company = new CompanyInfo { CompanyId = "C001", DefaultCurrency = "JPY" };
            var ctx = Ctx(company);

            // 原幣 USD 金額 100.00，匯率 150.5（preserve 全精度）→ 本幣 JPY 0 位
            decimal amount = 100.00m;
            decimal rate = 150.5m;
            decimal home = NumberFormatResolver.RoundByKind(amount * rate, NumberKind.Amount, ctx, "JPY");

            Assert.Equal(15050m, home); // 100 × 150.5 = 15050 → JPY 0 位
        }

        [Fact]
        [DisplayName("同列原幣/本幣不同幣：各依自己 CUKY 欄位數解析")]
        public void SameRow_OriginalAndHome_DifferentCurrencies()
        {
            var ctx = Ctx();

            // 原幣 USD 2 位、本幣 JPY 0 位
            Assert.Equal(2, NumberFormatResolver.ResolveDecimals(NumberKind.Amount, ctx, "USD"));
            Assert.Equal(0, NumberFormatResolver.ResolveDecimals(NumberKind.Amount, ctx, "JPY"));
        }

        [Fact]
        [DisplayName("Preserve：單價/匯率不因幣別捨入，用完整精度")]
        public void Preserve_UnitPriceAndRate_NotRounded()
        {
            var ctx = Ctx();
            var value = 12.3456789m;

            Assert.Equal(value, NumberFormatResolver.RoundByKind(value, NumberKind.UnitPrice, ctx, "JPY"));
            Assert.Equal(value, NumberFormatResolver.RoundByKind(value, NumberKind.ExchangeRate, ctx, "JPY"));
        }
    }
}
