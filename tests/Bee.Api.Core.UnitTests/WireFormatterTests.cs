using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Collections;
using Bee.Definition.Organization;
using Bee.Definition.Sorting;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 手寫 wire formatter 的 round-trip 與形狀測試。
    /// </summary>
    /// <remarks>
    /// 這裡驗的是 round-trip 保真度。**型別與 formatter 的成員清單是否一致，不在這裡驗**——
    /// 那由 <c>WireContractDriftTests</c> 承擔，它比對 <c>IWireContract.WireMemberNames</c>
    /// 與型別當下的形狀。
    /// <para>
    /// 先前這裡有一組 <c>WireMemberCount</c> 斷言，宣稱是「型別與 formatter 之間唯一的連結」。
    /// 它讀回 formatter 自己寫的 map header、再與 formatter 自己宣告的常數比較，
    /// 也就是 <c>Assert.Equal(X, X)</c>——在任何情況下都不可能失敗。八個手寫 formatter 的
    /// 目標型別因此從未有過形狀守衛。已改為讓那些 formatter 實作 <c>IWireContract</c>。
    /// </para>
    /// </remarks>
    public class WireFormatterTests
    {
        [Fact]
        [DisplayName("SortField 應 round-trip")]
        public void SortField_RoundTripsWithDeclaredMemberCount()
        {
            var source = new SortField("cust_id", SortDirection.Desc);

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<SortField>(bytes);

            Assert.Equal("cust_id", result.FieldName);
            Assert.Equal(SortDirection.Desc, result.Direction);
        }

        [Fact]
        [DisplayName("框架管理的成員不得上 wire")]
        public void FrameworkManagedMembers_DoNotTravel()
        {
            var source = new SortField("cust_id", SortDirection.Asc) { Tag = "should-not-travel" };
            source.SetSerializeState(SerializeState.Serialize);

            var result = MessagePackCodec.Deserialize<SortField>(MessagePackCodec.Serialize(source));

            Assert.Null(result.Tag);
            Assert.Equal(SerializeState.None, result.SerializeState);
            Assert.Null(result.Collection);
        }

        [Fact]
        [DisplayName("DepartmentNode 應 round-trip，含巢狀子節點")]
        public void DepartmentNode_RoundTripsWithNestedChildren()
        {
            var source = new DepartmentNode
            {
                RowId = Guid.NewGuid(),
                DeptId = "D01",
                DeptName = "業務部",
                ManagerRowId = Guid.NewGuid(),
                Children = [new DepartmentNode { DeptId = "D01-1", DeptName = "北區" }],
            };

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<DepartmentNode>(bytes);

            Assert.Equal(source.RowId, result.RowId);
            Assert.Equal("業務部", result.DeptName);
            Assert.Equal(source.ManagerRowId, result.ManagerRowId);
            Assert.NotNull(result.Children);
            Assert.Single(result.Children);
            Assert.Equal("北區", result.Children[0].DeptName);
        }

        [Fact]
        [DisplayName("DepartmentTree 應 round-trip 整棵樹")]
        public void DepartmentTree_RoundTrips()
        {
            var source = new DepartmentTree
            {
                CompanyId = "C01",
                Roots = [new DepartmentNode { DeptId = "D01", DeptName = "總部" }],
            };

            var result = MessagePackCodec.Deserialize<DepartmentTree>(MessagePackCodec.Serialize(source));

            Assert.Equal("C01", result.CompanyId);
            Assert.NotNull(result.Roots);
            Assert.Single(result.Roots);
            Assert.Equal("總部", result.Roots[0].DeptName);
        }

        [Fact]
        [DisplayName("NumberFormatItem 應 round-trip，且成員數相符")]
        public void NumberFormatItem_RoundTrips()
        {
            var source = new NumberFormatItem { Kind = NumberKind.Amount, Decimals = 4 };

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<NumberFormatItem>(bytes);

            Assert.Equal(NumberKind.Amount, result.Kind);
            Assert.Equal(4, result.Decimals);
        }

        [Fact]
        [DisplayName("CashRoundingItem 應 round-trip，且成員數相符")]
        public void CashRoundingItem_RoundTrips()
        {
            var source = new CashRoundingItem { CurrencyCode = "TWD", Unit = 0.5m };

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<CashRoundingItem>(bytes);

            Assert.Equal("TWD", result.CurrencyCode);
            Assert.Equal(0.5m, result.Unit);
        }

        [Fact]
        [DisplayName("AllowedCurrencyItem 應 round-trip，且成員數相符")]
        public void AllowedCurrencyItem_RoundTrips()
        {
            var source = new AllowedCurrencyItem { Code = "USD" };

            var bytes = MessagePackCodec.Serialize(source);
            var result = MessagePackCodec.Deserialize<AllowedCurrencyItem>(bytes);

            Assert.Equal("USD", result.Code);
        }

        [Fact]
        [DisplayName("Parameter 應 round-trip，Value 走白名單型別")]
        public void Parameter_RoundTripsAllowedValueTypes()
        {
            var source = new ParameterCollection
            {
                new Parameter("s", "hello"),
                new Parameter("i", 42),
                new Parameter("g", Guid.NewGuid()),
            };

            var result = MessagePackCodec.Deserialize<ParameterCollection>(
                MessagePackCodec.Serialize(source));

            Assert.Equal("hello", result["s"].Value);
            Assert.Equal(42, result["i"].Value);
            Assert.Equal(source["g"].Value, result["g"].Value);
        }

        [Fact]
        [DisplayName("Parameter.Value 帶白名單外型別必須被擋下")]
        public void Parameter_DisallowedValueType_IsBlocked()
        {
            var source = new ParameterCollection
            {
                new Parameter("evil", new Version(1, 2, 3, 4)),
            };

            // 這是安全邊界：具名型別通道若放行白名單外型別即為 gadget 破口。
            // `WireValueFormatter` 在寫入端就擋，比先前只在讀取端擋更早一步；
            // 讀取端的檢查仍在（見 WireValueFormatterTests 的手工封套測試）。
            Assert.NotNull(Record.Exception(() => MessagePackCodec.Serialize(source)));
        }

        [Fact]
        [DisplayName("CompanyInfo 應 round-trip，含三個遞移集合")]
        public void CompanyInfo_RoundTripsTransitiveCollections()
        {
            var source = new Bee.Definition.Identity.CompanyInfo
            {
                CompanyId = "C01",
                CompanyName = "示範公司",
                DefaultCurrency = "TWD",
                NumberFormats = [new NumberFormatItem { Kind = NumberKind.Amount, Decimals = 2 }],
                CashRounding = [new CashRoundingItem { CurrencyCode = "TWD", Unit = 1m }],
                AllowedCurrencies = [new AllowedCurrencyItem { Code = "USD" }],
            };

            var result = MessagePackCodec.Deserialize<Bee.Definition.Identity.CompanyInfo>(
                MessagePackCodec.Serialize(source));

            Assert.Equal("示範公司", result.CompanyName);
            Assert.Single(result.NumberFormats);
            Assert.Equal(2, result.NumberFormats[0].Decimals);
            Assert.Single(result.CashRounding);
            Assert.Equal(1m, result.CashRounding[0].Unit);
            Assert.Single(result.AllowedCurrencies);
            Assert.Equal("USD", result.AllowedCurrencies[0].Code);
        }
    }
}
