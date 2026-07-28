using System.ComponentModel;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.UnitTests.Contracts
{
    /// <summary>
    /// 守住「契約軸對稱性」：<c>Bee.Api.Core.Messages.*</c> 下每個 wire 型別
    /// （<see cref="ApiRequest"/>／<see cref="ApiResponse"/> 子型別）都必須實作
    /// <c>Bee.Api.Contracts</c> 中的同名 <c>I*</c> 契約介面。
    /// </summary>
    /// <remarks>
    /// 此對稱性過去只靠人工維持。2026-07-28 的體檢發現 <c>GetDepartmentTreeRequest</c>
    /// 漏了契約介面，而上一輪體檢曾宣稱此軸「100% 對齊」—— 破了兩個月無人察覺，
    /// 因為沒有任何測試守著。本測試即為該缺口的補強。
    /// </remarks>
    public class ApiContractPairingTests
    {
        /// <summary>
        /// 取得所有需要配對契約介面的 wire 型別。
        /// </summary>
        public static TheoryData<Type> WireMessageTypes()
        {
            var data = new TheoryData<Type>();
            foreach (var type in typeof(ApiRequest).Assembly.GetTypes()
                .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract)
                .Where(t => typeof(ApiRequest).IsAssignableFrom(t) || typeof(ApiResponse).IsAssignableFrom(t))
                .OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                data.Add(type);
            }
            return data;
        }

        [Theory]
        [MemberData(nameof(WireMessageTypes))]
        [DisplayName("每個 wire 訊息型別都應實作同名的 I* 契約介面")]
        public void WireMessageType_ImplementsMatchingContractInterface(Type wireType)
        {
            var expectedName = "I" + wireType.Name;

            var implemented = wireType.GetInterfaces()
                .Any(i => string.Equals(i.Name, expectedName, StringComparison.Ordinal));

            Assert.True(implemented,
                $"{wireType.FullName} 未實作契約介面 {expectedName}。" +
                "每個 wire 訊息型別都必須有配對的契約介面（見 Bee.Api.Contracts）。");
        }

        [Fact]
        [DisplayName("wire 訊息型別的列舉不應為空")]
        public void WireMessageTypes_IsNotEmpty()
        {
            // 防止上面的 Theory 因反射條件寫錯而變成零案例的假綠燈
            Assert.NotEmpty(WireMessageTypes());
        }
    }
}
