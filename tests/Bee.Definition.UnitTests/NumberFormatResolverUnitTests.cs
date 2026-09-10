using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// NumberFormatResolver 計量單位：數量/重量依 UNIT 欄單位解析位數、同單位 round-then-sum；
    /// 無單位碼不捨入、單位碼不在主檔或無主檔時退框架預設，公司位數一律不參與。
    /// </summary>
    public class NumberFormatResolverUnitTests
    {
        private static UnitSettings Units() =>
        [
            new UnitItem("PCS", 0, "count", "Pieces"),
            new UnitItem("KG", 3, "weight", "Kilogram"),
            new UnitItem("M", 2, "length", "Metre"),
        ];

        private static RoundingContext Ctx(CompanyInfo? company = null) =>
            new() { Company = company, UnitSettings = Units() };

        private static CompanyInfo CompanyWithOverride(NumberKind kind, int decimals)
        {
            var company = new CompanyInfo { CompanyId = "C001" };
            company.NumberFormats.Add(new NumberFormatItem(kind, decimals));
            return company;
        }

        [Theory]
        [InlineData(NumberKind.Quantity, "PCS", 0)]
        [InlineData(NumberKind.Quantity, "KG", 3)]
        [InlineData(NumberKind.Weight, "KG", 3)]
        [InlineData(NumberKind.Weight, "M", 2)]
        [DisplayName("ResolveDecimals 數量/重量依 UNIT 欄單位給不同位數")]
        public void ResolveDecimals_ByUnit(NumberKind kind, string code, int expected)
        {
            Assert.Equal(expected, NumberFormatResolver.ResolveDecimals(kind, Ctx(), code));
        }

        [Theory]
        [InlineData("PCS", "N0")]
        [InlineData("KG", "N3")]
        [DisplayName("ResolveFormat 數量依單位給不同格式字串")]
        public void ResolveFormat_ByUnit(string code, string expected)
        {
            Assert.Equal(expected, NumberFormatResolver.ResolveFormat(NumberKind.Quantity, Ctx(), code));
        }

        [Theory]
        [InlineData(NumberKind.Quantity, 0)]
        [InlineData(NumberKind.Weight, 3)]
        [DisplayName("ResolveDecimals 無單位碼時回框架預設，公司覆寫不參與")]
        public void ResolveDecimals_NoUnit_IgnoresCompanyOverride(NumberKind kind, int expected)
        {
            var ctx = Ctx(CompanyWithOverride(kind, 5));

            Assert.Equal(expected, NumberFormatResolver.ResolveDecimals(kind, ctx, null));
        }

        [Theory]
        [InlineData(NumberKind.Quantity, null)]
        [InlineData(NumberKind.Quantity, "")]
        [InlineData(NumberKind.Weight, null)]
        [InlineData(NumberKind.Weight, "")]
        [DisplayName("RoundByKind 列單位碼空時原值返回（不捨入），即使公司設了覆寫")]
        public void RoundByKind_NoUnit_ReturnsValueUnchanged(NumberKind kind, string? unitCode)
        {
            var ctx = Ctx(CompanyWithOverride(kind, 0));

            Assert.Equal(1.23456m, NumberFormatResolver.RoundByKind(1.23456m, kind, ctx, unitCode));
        }

        [Fact]
        [DisplayName("ResolveDecimals 無單位主檔時退框架預設（Quantity 0、Weight 3）")]
        public void ResolveDecimals_NoUnitMaster_FrameworkDefault()
        {
            var ctx = new RoundingContext { Company = null, UnitSettings = null };

            Assert.Equal(0, NumberFormatResolver.ResolveDecimals(NumberKind.Quantity, ctx, "KG"));
            Assert.Equal(3, NumberFormatResolver.ResolveDecimals(NumberKind.Weight, ctx, "KG"));
        }

        [Fact]
        [DisplayName("ResolveDecimals 無單位主檔時公司覆寫不參與（Quantity 公司設 2 → 框架 0）")]
        public void ResolveDecimals_NoUnitMaster_IgnoresCompanyOverride()
        {
            var ctx = new RoundingContext { Company = CompanyWithOverride(NumberKind.Quantity, 2), UnitSettings = null };

            Assert.Equal(0, NumberFormatResolver.ResolveDecimals(NumberKind.Quantity, ctx, "KG"));
        }

        [Theory]
        [InlineData(NumberKind.Quantity, 0)]
        [InlineData(NumberKind.Weight, 3)]
        [DisplayName("ResolveDecimals 單位碼不在主檔時退框架預設（Quantity 0、Weight 3）")]
        public void ResolveDecimals_UnknownUnit_FrameworkDefault(NumberKind kind, int expected)
        {
            Assert.Equal(expected, NumberFormatResolver.ResolveDecimals(kind, Ctx(), "XXX"));
        }

        [Fact]
        [DisplayName("RoundByKind 單位碼不在主檔時依框架預設捨入（Weight 3 位、Quantity 0 位）")]
        public void RoundByKind_UnknownUnit_RoundsToFrameworkDefault()
        {
            Assert.Equal(1.235m, NumberFormatResolver.RoundByKind(1.2345m, NumberKind.Weight, Ctx(), "XXX"));
            Assert.Equal(2m, NumberFormatResolver.RoundByKind(1.5m, NumberKind.Quantity, Ctx(), "XXX"));
        }

        [Fact]
        [DisplayName("ResolveDecimals 公司多載：數量回框架預設，公司覆寫不參與")]
        public void ResolveDecimals_CompanyOverload_Quantity_FrameworkDefault()
        {
            var company = CompanyWithOverride(NumberKind.Quantity, 2);

            Assert.Equal(0, NumberFormatResolver.ResolveDecimals(NumberKind.Quantity, company));
        }

        [Fact]
        [DisplayName("RoundByKind 公司多載：數量沒有單位碼 → 原值返回")]
        public void RoundByKind_CompanyOverload_Quantity_ReturnsValueUnchanged()
        {
            var company = CompanyWithOverride(NumberKind.Quantity, 0);

            Assert.Equal(1.5m, NumberFormatResolver.RoundByKind(1.5m, NumberKind.Quantity, company));
        }

        [Fact]
        [DisplayName("RoundByKind 數量依單位捨入（PCS 0 位 / KG 3 位）")]
        public void RoundByKind_ByUnit()
        {
            Assert.Equal(12m, NumberFormatResolver.RoundByKind(12.345m, NumberKind.Quantity, Ctx(), "PCS"));
            Assert.Equal(12.345m, NumberFormatResolver.RoundByKind(12.3454m, NumberKind.Weight, Ctx(), "KG"));
        }

        [Fact]
        [DisplayName("round-then-sum：同欄 KG（3 位）明細各捨入後加總 == 表頭合計")]
        public void RoundThenSum_SameUnit_InvariantHolds()
        {
            decimal[] details = [1.2345m, 1.2345m, 1.2345m];
            var ctx = Ctx();

            decimal total = 0m;
            foreach (var d in details)
                total += NumberFormatResolver.RoundByKind(d, NumberKind.Weight, ctx, "KG");

            // 每筆 1.2345 → KG 3 位 AwayFromZero → 1.235；×3 = 3.705。
            Assert.Equal(3.705m, total);

            // 對照：全精度加總後才捨（禁止做法）= round(3.7035, KG) = 3.704 ≠ 3.705。
            decimal sumThenRound = NumberFormatResolver.RoundByKind(
                details[0] + details[1] + details[2], NumberKind.Weight, ctx, "KG");
            Assert.NotEqual(total, sumThenRound);
        }

        [Fact]
        [DisplayName("同欄不同列不同單位 → 各依自己單位位數解析（PCS 0 vs KG 3）")]
        public void SameColumn_DifferentUnits_DifferentDecimals()
        {
            var ctx = Ctx();

            Assert.Equal(0, NumberFormatResolver.ResolveDecimals(NumberKind.Quantity, ctx, "PCS"));
            Assert.Equal(3, NumberFormatResolver.ResolveDecimals(NumberKind.Quantity, ctx, "KG"));
        }
    }
}
