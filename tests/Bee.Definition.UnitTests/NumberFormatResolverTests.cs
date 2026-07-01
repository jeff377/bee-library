using System.ComponentModel;
using Bee.Definition.Identity;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// NumberFormatResolver：公司感知位數/格式解析與 RoundByKind（含 round-then-sum 鐵則）。
    /// </summary>
    public class NumberFormatResolverTests
    {
        private static CompanyInfo Company(params NumberFormatItem[] overrides)
        {
            var company = new CompanyInfo { CompanyId = "C001" };
            foreach (var item in overrides)
                company.NumberFormats.Add(item);
            return company;
        }

        [Fact]
        [DisplayName("ResolveDecimals 公司覆寫優先於框架預設")]
        public void ResolveDecimals_CompanyOverride_Wins()
        {
            var company = Company(new NumberFormatItem(NumberKind.Percent, 4));

            Assert.Equal(4, NumberFormatResolver.ResolveDecimals(NumberKind.Percent, company));
        }

        [Fact]
        [DisplayName("ResolveDecimals company 為 null 時退框架預設")]
        public void ResolveDecimals_NullCompany_FrameworkDefault()
        {
            Assert.Equal(2, NumberFormatResolver.ResolveDecimals(NumberKind.Amount, null));
            Assert.Equal(4, NumberFormatResolver.ResolveDecimals(NumberKind.UnitPrice, null));
        }

        [Fact]
        [DisplayName("ResolveDecimals SystemFixed（匯率）忽略公司覆寫，永遠框架預設")]
        public void ResolveDecimals_SystemFixed_IgnoresCompanyOverride()
        {
            // 即使公司對 ExchangeRate 設了覆寫，SystemFixed 也不採用。
            var company = Company(new NumberFormatItem(NumberKind.ExchangeRate, 9));

            Assert.Equal(5, NumberFormatResolver.ResolveDecimals(NumberKind.ExchangeRate, company));
        }

        [Theory]
        [InlineData(NumberKind.Amount, "N2")]
        [InlineData(NumberKind.Percent, "P2")]
        [InlineData(NumberKind.ExchangeRate, "N5")]
        [DisplayName("ResolveFormat null company 時回框架預設格式字串")]
        public void ResolveFormat_NullCompany_FrameworkFormat(NumberKind kind, string expected)
        {
            Assert.Equal(expected, NumberFormatResolver.ResolveFormat(kind, null));
        }

        [Fact]
        [DisplayName("ResolveFormat 公司覆寫反映於格式字串")]
        public void ResolveFormat_CompanyOverride_Reflected()
        {
            var company = Company(new NumberFormatItem(NumberKind.UnitPrice, 6));

            Assert.Equal("N6", NumberFormatResolver.ResolveFormat(NumberKind.UnitPrice, company));
        }

        [Theory]
        [InlineData(NumberKind.UnitPrice)]
        [InlineData(NumberKind.Cost)]
        [InlineData(NumberKind.ExchangeRate)]
        [DisplayName("RoundByKind Preserve 類原值返回（不捨入）")]
        public void RoundByKind_Preserve_ReturnsOriginal(NumberKind kind)
        {
            var value = 12.3456789m;

            Assert.Equal(value, NumberFormatResolver.RoundByKind(value, kind, null));
        }

        [Fact]
        [DisplayName("RoundByKind Round 類以 AwayFromZero 捨到解析位數")]
        public void RoundByKind_Round_AwayFromZero()
        {
            // Amount 預設 2 位；12.345 → 12.35（away from zero，非 banker's rounding）
            Assert.Equal(12.35m, NumberFormatResolver.RoundByKind(12.345m, NumberKind.Amount, null));
            // 負值同樣遠離零
            Assert.Equal(-12.35m, NumberFormatResolver.RoundByKind(-12.345m, NumberKind.Amount, null));
        }

        [Fact]
        [DisplayName("RoundByKind 依公司位數捨入（Percent 公司設 0 位）")]
        public void RoundByKind_UsesCompanyDecimals()
        {
            var company = Company(new NumberFormatItem(NumberKind.Percent, 0));

            Assert.Equal(13m, NumberFormatResolver.RoundByKind(12.5m, NumberKind.Percent, company));
        }

        [Fact]
        [DisplayName("Round-then-sum 鐵則：先各自捨入再加總 ≠ 全精度加總後才捨")]
        public void RoundThenSum_DiffersFromSumThenRound()
        {
            decimal[] details = [10.333m, 10.333m, 10.333m];

            // round-then-sum：每筆先捨到 Amount 位數（2）再加總
            decimal roundedSum = 0m;
            foreach (var d in details)
                roundedSum += NumberFormatResolver.RoundByKind(d, NumberKind.Amount, null);

            // sum-then-round：全精度加總後才捨（禁止的做法）
            decimal sumThenRound = NumberFormatResolver.RoundByKind(
                details[0] + details[1] + details[2], NumberKind.Amount, null);

            Assert.Equal(30.99m, roundedSum);      // 10.33 × 3
            Assert.Equal(31.00m, sumThenRound);    // 30.999 → 31.00
            Assert.NotEqual(roundedSum, sumThenRound);
        }
    }
}
