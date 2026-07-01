using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Identity;

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
    }
}
