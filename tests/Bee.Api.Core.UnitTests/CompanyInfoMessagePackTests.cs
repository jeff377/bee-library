using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Definition;
using Bee.Definition.Identity;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 驗證 CompanyInfo（含 CompanyNumberFormats 集合）的 MessagePack wire round-trip。
    /// CompanyInfo 經 IEnterCompanyResponse.Company 走 MessagePack，CompanyNumberFormats
    /// 須由自訂 FormatterResolver 的 CollectionBaseFormatter 處理。
    /// </summary>
    public sealed class CompanyInfoMessagePackTests
    {
        [Fact]
        [DisplayName("CompanyInfo 含數值覆寫項 MessagePack round-trip 應保留覆寫與基本欄位")]
        public void CompanyInfo_WithNumberFormats_RoundTrip_Succeeds()
        {
            var original = new CompanyInfo
            {
                CompanyId = "C001",
                CompanyName = "測試公司",
                CompanyDatabaseId = "common",
                CustomizeId = "",
                NumberFormats =
                [
                    new NumberFormatItem(NumberKind.Percent, 3),
                    new NumberFormatItem(NumberKind.UnitPrice, 6),
                ],
            };

            var bytes = MessagePackCodec.Serialize(original);
            Assert.NotNull(bytes);
            Assert.NotEmpty(bytes);

            var restored = MessagePackCodec.Deserialize<CompanyInfo>(bytes);

            Assert.NotNull(restored);
            Assert.Equal("C001", restored!.CompanyId);
            Assert.Equal("測試公司", restored.CompanyName);
            Assert.Equal(2, restored.NumberFormats.Count);
            Assert.Equal(3, restored.GetDecimals(NumberKind.Percent));
            Assert.Equal(6, restored.GetDecimals(NumberKind.UnitPrice));
            // 未覆寫者退框架預設
            Assert.Equal(2, restored.GetDecimals(NumberKind.Amount));
        }

        [Fact]
        [DisplayName("CompanyInfo 空數值覆寫表 MessagePack round-trip 應回空表並全退框架預設")]
        public void CompanyInfo_EmptyNumberFormats_RoundTrip_Succeeds()
        {
            var original = new CompanyInfo { CompanyId = "C001", CompanyName = "測試公司" };

            var bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<CompanyInfo>(bytes);

            Assert.NotNull(restored);
            Assert.Empty(restored!.NumberFormats);
            Assert.Equal(4, restored.GetDecimals(NumberKind.UnitPrice));
        }
    }
}
